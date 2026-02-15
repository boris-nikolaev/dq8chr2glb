using System;
using System.Collections.Generic;
using System.Numerics;
using dq8chr2glb.Core.MDSFormat;
using dq8chr2glb.Logger;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace dq8chr2glb.Converter.GLTF
{
    public static class UniversalMeshBuilder
    {
        public static Mesh CreateMesh(ModelRoot _root, MDSMesh mdsMesh, MDSMaterial[] materials,
                                      Dictionary<string, MaterialBuilder> materialCache, Dictionary<int, int> nodesMap)
        {
            var hasUvs = (mdsMesh.features & MeshFeatures.UVs) != 0;
            var hasSkinning = (mdsMesh.features & MeshFeatures.Weights) != 0;

            if (hasSkinning)
            {
                if (hasUvs)
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexTexture1, VertexJoints4>(
                     _root, mdsMesh, materials, materialCache, nodesMap);
                }
                else
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexEmpty, VertexJoints4>(
                     _root, mdsMesh, materials, materialCache, nodesMap);
                }
            }
            else
            {
                if (hasUvs)
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexTexture1, VertexEmpty>(
                     _root, mdsMesh, materials, materialCache, nodesMap);
                }
                else
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexEmpty, VertexEmpty>(
                     _root, mdsMesh, materials, materialCache, nodesMap);
                }
            }
        }

        private static (bool hasUvs, bool hasSkinning) AnalyzeMeshAttributes(MDSMesh mdsMesh)
        {
            var hasUvs = mdsMesh.uv != null && mdsMesh.uv.Length > 0 &&
                         Array.Exists(mdsMesh.uv, uv => uv != null && uv.Length >= 2);
            var hasSkinning = mdsMesh.weights != null && mdsMesh.bones != null &&
                              mdsMesh.bones.Length > 0;

            return (hasUvs, hasSkinning);
        }

        private static Mesh CreateMeshWithAllAttributes<TvG, TvM, TvS>(
            ModelRoot root,
            MDSMesh mdsMesh,
            MDSMaterial[] materials,
            Dictionary<string, MaterialBuilder> materialCache,
            Dictionary<int, int> nodesMap)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var meshBuilder = new MeshBuilder<MaterialBuilder, TvG, TvM, TvS>(mdsMesh.name);

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var skinningData = new List<(int, float)[]>();
            var hasUvs = (mdsMesh.features & MeshFeatures.UVs) != 0;
            var hasSkinning = (mdsMesh.features & MeshFeatures.Weights) != 0;

            for (var i = 0; i < mdsMesh.vertices.Length; i++)
            {
                positions.Add(new Vector3(
                                          mdsMesh.vertices[i][0],
                                          mdsMesh.vertices[i][1],
                                          mdsMesh.vertices[i][2]));

                normals.Add(Vector3.Zero);

                if (hasUvs && i < mdsMesh.uv.Length && mdsMesh.uv[i] != null && mdsMesh.uv[i].Length >= 2)
                {
                    texCoords.Add(new Vector2(mdsMesh.uv[i][0], mdsMesh.uv[i][1]));
                }
                else
                {
                    texCoords.Add(Vector2.Zero);
                }

                if (hasSkinning && i < mdsMesh.bones.Length)
                {
                    var boneIndices = mdsMesh.bones[i];
                    var boneWeights = mdsMesh.weights[i];

                    var bindings = new (int JointIndex, float Weight)[Math.Min(4, boneIndices.Length)];

                    var wSum = 0f;
                    for (var b = 0; b < bindings.Length; b++)
                    {
                        var mdsBoneIndex = boneIndices[b];
                        if (nodesMap.ContainsKey(mdsBoneIndex))
                        {
                            var gltfBoneIndex = nodesMap[mdsBoneIndex];
                            bindings[b] = (Math.Max(gltfBoneIndex, 0), boneWeights[b]);
                            wSum += boneWeights[b];
                        }
                        else
                        {
                            bindings[b] = (0, boneWeights[b]);
                        }
                    }

                    if (wSum < 0.00001f)
                    {
                        skinningData.Add(Array.Empty<(int, float)>());
                        continue;
                    }

                    for (var index = 0; index < bindings.Length; index++)
                    {
                        bindings[index].Weight /= wSum;
                    }

                    skinningData.Add(bindings);
                }
                else
                {
                    skinningData.Add(Array.Empty<(int, float)>());
                }
            }

            MeshUtils.ComputeNormals(positions, mdsMesh.triangles, normals);

            foreach (var submesh in mdsMesh.submeshes)
            {
                var materialBuilder = materialCache.TryGetValue(
                                                                submesh.materialIndex >= 0 &&
                                                                submesh.materialIndex < materials.Length
                                                                    ? materials[submesh.materialIndex].name
                                                                    : "default",
                                                                out var mat)
                    ? mat
                    : materialCache["default"];

                var primitive = meshBuilder.UsePrimitive(materialBuilder);

                var indices = new List<int>();
                for (var i = submesh.startIndex; i < submesh.startIndex + submesh.indexCount; i += 3)
                {
                    indices.Add(mdsMesh.triangles[i]);
                    indices.Add(mdsMesh.triangles[i + 1]);
                    indices.Add(mdsMesh.triangles[i + 2]);
                }

                var maxBoneIndex = 0;
                foreach (var bones in mdsMesh.bones)
                {
                    foreach (var boneID in bones)
                    {
                        maxBoneIndex = Math.Max(boneID, maxBoneIndex);
                    }
                }

                for (var i = 0; i < indices.Count; i += 3)
                {
                    var idxA = indices[i];
                    var idxB = indices[i + 1];
                    var idxC = indices[i + 2];

                    var vertexA = CreateVertex<TvG, TvM, TvS>(
                                                              positions[idxA],
                                                              normals[idxA],
                                                              texCoords.Count > idxA ? texCoords[idxA] : Vector2.Zero,
                                                              skinningData.Count != 0 ? skinningData[idxA] : null);

                    var vertexB = CreateVertex<TvG, TvM, TvS>(
                                                              positions[idxB],
                                                              normals[idxB],
                                                              texCoords.Count > idxB ? texCoords[idxB] : Vector2.Zero,
                                                              skinningData.Count != 0 ? skinningData[idxB] : null);

                    var vertexC = CreateVertex<TvG, TvM, TvS>(
                                                              positions[idxC],
                                                              normals[idxC],
                                                              texCoords.Count > idxC ? texCoords[idxC] : Vector2.Zero,
                                                              skinningData.Count != 0 ? skinningData[idxC] : null);

                    primitive.AddTriangle(vertexA, vertexB, vertexC);
                }
            }

            try
            {
                return root.CreateMesh(meshBuilder);
            }
            catch (Exception e)
            {
                Log.Line($"Create Mesh Error: {mdsMesh.name}", LogLevel.Error);
                Log.Error(e);
                return null;
            }
        }

        private static VertexBuilder<TvG, TvM, TvS> CreateVertex<TvG, TvM, TvS>(
            Vector3 position,
            Vector3 normal,
            Vector2 texCoord,
            (int, float)[] skinningData)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var geometry = default(TvG);
            geometry.SetPosition(position);
            geometry.SetNormal(normal);

            var material = default(TvM);
            if (typeof(TvM) != typeof(VertexEmpty))
            {
                try
                {
                    material.SetTexCoord(0, texCoord);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            var skinning = default(TvS);
            if (skinningData != null && typeof(TvS) != typeof(VertexEmpty))
            {
                try
                {
                    skinning.SetBindings(skinningData);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            return new VertexBuilder<TvG, TvM, TvS>(geometry, material, skinning);
        }
    }
}