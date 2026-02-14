using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using dq8chr2glb.Core.InfoCfg;
using dq8chr2glb.Core.MDSFormat;
using dq8chr2glb.Core.MOTFormat;
using dq8chr2glb.Logger;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using SixLabors.ImageSharp;
using Node = SharpGLTF.Schema2.Node;
using Texture = dq8chr2glb.TM2Format.Texture;

namespace dq8chr2glb.Converter;

public class MDSConverter
{
    private readonly string _name;
    private readonly ModelRoot _gltfModel;
    private readonly Dictionary<int, Node> _nodeMap = new();
    private readonly List<Node> _boneNodes = new();
    private readonly Dictionary<string, MaterialBuilder> _materialCache = new();
    private Dictionary<string, ImageBuilder> _textureBuilders = new();

    private const float FRAMERATE = 16f;

    public MDSConverter(string name)
    {
        _name = name;
        _gltfModel = ModelRoot.CreateModel();
    }

    public void Convert(MDSScene scene, List<Texture> images, string name)
    {
        var gltfScene = _gltfModel.UseScene("DefaultScene");

        var nodesWithParents = new HashSet<int>();

        if (scene.nodes == null || scene.nodes.Length == 0)
        {
            Log.Line($"There is nothing to convert in {name}", LogLevel.Info);
            return;
        }

        foreach (var node in scene.nodes)
        {
            if (node.parentIndex >= 0)
            {
                nodesWithParents.Add(node.index);
            }
        }

        foreach (var node in scene.nodes)
        {
            if (node.parentIndex < 0)
            {
                var gltfNode = gltfScene.CreateNode(node.name);
                _nodeMap[node.index] = gltfNode;

                if (node is MDSBone)
                {
                    _boneNodes.Add(gltfNode);
                }

                gltfNode.WithLocalTransform(ToNumericsMatrix4x4(node.transform));
            }
        }

        foreach (var node in scene.nodes)
        {
            if (node.parentIndex >= 0 && _nodeMap.ContainsKey(node.parentIndex))
            {
                var parent = _nodeMap[node.parentIndex];
                var gltfNode = parent.CreateNode(node.name);
                _nodeMap[node.index] = gltfNode;

                if (node is MDSBone)
                {
                    _boneNodes.Add(gltfNode);
                }

                gltfNode.WithLocalTransform(ToNumericsMatrix4x4(node.transform));
            }
        }

        var nodesMapping = new Dictionary<int, int>();
        for (int i = 0; i < _boneNodes.Count; i++)
        {
            var boneNode = _boneNodes[i];
            var mdsNode = Array.Find(scene.nodes, n => n.name == boneNode.Name);
            if (mdsNode != null)
            {
                nodesMapping[mdsNode.index] = i;
            }
        }

        CreateMaterials(scene.materials, images);

        var meshes = scene.nodes.Where(n => n is MDSMesh).Cast<MDSMesh>().ToArray();
        foreach (var mdsMesh in meshes)
        {
            if (mdsMesh?.vertices == null || mdsMesh.triangles == null) continue;

            var mesh = CreateGltfMesh(mdsMesh, scene.materials, nodesMapping);
            if (_nodeMap.TryGetValue(mdsMesh.index, out var meshNode))
            {
                meshNode.WithMesh(mesh);
            }
        }

        if (_boneNodes.Count > 0)
        {
            SetupSkeleton(scene.nodes, name);
        }
    }

    public void CreateAnimation(List<MotionCurve> motionCurves, ModelConfig config)
    {
        foreach (var clip in config.clips)
        {
            var rotationCurves = new List<(Node node, Dictionary<float, Quaternion> items)>();
            var translationCurves = new List<(Node node, Dictionary<float, Vector3> items)>();
            foreach (var curve in motionCurves)
            {
                if (_nodeMap.TryGetValue(curve.boneIndex, out var node))
                {
                    var rotationKeyframes = new Dictionary<float, Quaternion>();
                    var translationKeyframes = new Dictionary<float, Vector3>();

                    for (var i = clip.startFrame; i < clip.endFrame; i++)
                    {
                        if (i >= curve.keyframes.Count || curve.keyframes[i] == null)
                        {
                            continue;
                        }

                        var keyframe = curve.keyframes[i];

                        if (curve.curveType == KeyframeType.Quaternion)
                        {
                            var time = keyframe.frame / FRAMERATE / clip.speed;
                            rotationKeyframes[time] = keyframe.rotation;
                        }
                        else if (curve.curveType == KeyframeType.Translation)
                        {
                            var time = keyframe.frame / FRAMERATE / clip.speed;
                            translationKeyframes[time] = keyframe.translation;
                        }
                    }

                    if (rotationKeyframes.Count > 0)
                    {
                        rotationCurves.Add((node, rotationKeyframes));
                    }

                    if (translationKeyframes.Count > 0)
                    {
                        translationCurves.Add((node, translationKeyframes));
                    }
                }
            }

            if (rotationCurves.Count > 0 || translationCurves.Count > 0)
            {
                var animation = _gltfModel.CreateAnimation(clip.name);
                
                foreach (var (node, items) in rotationCurves)
                {
                    if (items.Count > 0 && node != null)
                    {
                        animation.CreateRotationChannel(node, items);
                    }
                }

                foreach (var (node, items) in translationCurves)
                {
                    if (items.Count > 0 && node != null)
                    {
                        animation.CreateTranslationChannel(node, items);
                    }
                }
            }
        }
    }

    private void CreateMaterials(MDSMaterial[] materials, List<TM2Format.Texture> textures)
    {
        foreach (var tex in textures)
        {
            if (tex == null)
            {
                continue;
            }

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                tex.data.SaveAsPng(ms);
                imageBytes = ms.ToArray();
            }

            var imageBuilder = ImageBuilder.From(new MemoryImage(imageBytes), tex.name);
            _textureBuilders[tex.name] = imageBuilder;
        }

        var defaultMat = new MaterialBuilder();
        _materialCache["default"] = defaultMat;

        foreach (var mat in materials)
        {
            var materialBuilder = new MaterialBuilder();
            materialBuilder.Name = mat.name;

            if (!string.IsNullOrEmpty(mat.textureName) && _textureBuilders.ContainsKey(mat.textureName))
            {
                var textureImage = _textureBuilders[mat.textureName];
                materialBuilder.WithBaseColor(textureImage);
            }

            _materialCache[mat.name] = materialBuilder;
        }
    }

    private Mesh CreateGltfMesh(MDSMesh mdsMesh, MDSMaterial[] materials, Dictionary<int, int> nodesMapping)
    {
        return UniversalMeshBuilder.CreateMesh(_gltfModel, mdsMesh, materials, _materialCache, nodesMapping);
    }

    private void SetupSkeleton(MDSNode[] nodes, string name)
    {
        Node rootBone = null;
        foreach (var boneNode in _boneNodes)
        {
            var mdsNode = Array.Find(nodes, n => n.name == boneNode.Name);
            if (mdsNode is MDSBone bone && bone.parentIndex < 0)
            {
                rootBone = boneNode;
                break;
            }
        }

        if (_boneNodes.Count == 0)
        {
            Log.Line("Skip mesh with 0 bones");
            return;
        }

        var skin = _gltfModel.CreateSkin(name);
        skin.Skeleton = rootBone;

        var jointBindings = new (Node Joint, Matrix4x4 InverseBindMatrix)[_boneNodes.Count];
        for (var i = 0; i < _boneNodes.Count; i++)
        {
            var boneNode = _boneNodes[i];
            var mdsNode = Array.Find(nodes, n => n.name == boneNode.Name);

            Matrix4x4 bindMatrix;
            if (mdsNode is MDSBone b)
            {
                var bindposeMatrix = ToNumericsMatrix4x4(b.bindpose);
                if (Matrix4x4.Invert(bindposeMatrix, out var bindposeInverse))
                {
                    bindMatrix = Matrix4x4.Multiply(bindposeInverse, Matrix4x4.Identity);
                }
                else
                {
                    bindMatrix = Matrix4x4.Identity;
                }

                jointBindings[i] = (boneNode, bindMatrix);
            }
        }

        try
        {
            skin.BindJoints(jointBindings);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return;
        }

        foreach (var node in _gltfModel.LogicalNodes)
        {
            if (node.Mesh != null)
            {
                var hasWeights = node.Mesh.Primitives.All(i => i.VertexAccessors.ContainsKey("WEIGHTS_0"));
                if (hasWeights)
                {
                    if (Quaternion.Dot(node.LocalTransform.GetDecomposed().Rotation, Quaternion.Identity) >= 0.999f)
                    {
                        node.LocalTransform = Matrix4x4.Identity;
                    }

                    try
                    {
                        node.Skin = skin;
                    }
                    catch (Exception e)
                    {
                        Log.Line(e.TargetSite.Name);
                    }
                }
            }
        }
    }

    private Matrix4x4 ToNumericsMatrix4x4(MDSMatrix m)
    {
        return new Matrix4x4(
                             m.m00, m.m01, m.m02, m.m03,
                             m.m10, m.m11, m.m12, m.m13,
                             m.m20, m.m21, m.m22, m.m23,
                             m.m30, m.m31, m.m32, m.m33
                            );
    }

    public void Save(string filePath, bool textFormat = true)
    {
        var path = Path.Combine(filePath, _name + (textFormat ? ".gltf" : ".glb"));

        try
        {
            if (textFormat)
            {
                _gltfModel.SaveGLTF(path);
            }
            else
            {
                _gltfModel.SaveGLB(path);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
