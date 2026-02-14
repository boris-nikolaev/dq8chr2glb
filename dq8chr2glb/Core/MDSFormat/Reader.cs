using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dq8chr2glb.Logger;

namespace dq8chr2glb.Core.MDSFormat;

public class Reader
{
    private const float gScale = 0.00003f; // Global Scale Constant

    public static MDSScene Read(byte[] data, string filename)
    {
        var offset = 0;
        var header = Utils.ReadStruct<MDSHeader>(data, ref offset, true);

        if (header.version != 200)
        {
            Console.WriteLine($"Unknown version: {header.version}");
            return null;
        }

        var scene = new MDSScene();

        var names = ReadNames(header, data);
        ReadNodes(scene, header, data, names);
        ReadMaterials(scene, header, data, names);

        var mdtHeaders = new MDTHeader[header.meshCount];
        for (var meshIndex = 0; meshIndex < header.meshCount; meshIndex++)
        {
            offset = header.offsetToMDT + mdtHeaders.Sum(h => h.totalSize);

            var mdtHeader = Utils.ReadStruct<MDTHeader>(data, ref offset);
            mdtHeaders[meshIndex] = mdtHeader;

            var mesh = Utils.GetMeshByIndex(scene, meshIndex);

            var hasVertices = mdtHeader.vertsCount != 0;
            var hasColors = mdtHeader.colorsCount != 0;
            var hasUVs = mdtHeader.uvCount != 0;
            var isSkinned = mdtHeader.bonesPerVertex != 0;

            var features = Utils.GetFeaturesFlags(hasVertices, hasColors, hasUVs, isSkinned);
            mesh.features = features;
            mesh.bounds = mdtHeader.bounds;

            var scale = new float[3]
                { mdtHeader.scaleX * gScale, mdtHeader.scaleY * gScale, mdtHeader.scaleZ * gScale };
            var uvScale = new float[2] { mdtHeader.uvScaleX, mdtHeader.uvScaleY };

            // read vertices array
            var vOffset = offset + mdtHeader.toVertexCount + mdtHeader.bytesToFirstVertex;
            var vCount = mdtHeader.vertsCount;
            var vertices = Utils.ReadArray<short[], float[]>(data, vOffset, vCount, 3,
                                                             s => new[]
                                                             {
                                                                 s[0] * scale[0], s[1] * scale[1], s[2] * scale[2]
                                                             });

            // read colors array
            var colors = new float[mdtHeader.colorsCount][];
            if ((features & MeshFeatures.Colors) == MeshFeatures.Colors)
            {
                var cOffset = offset + mdtHeader.toVertexCount + mdtHeader.bytesToFirstColor;
                var cCount = mdtHeader.colorsCount;
                var maxByte = (float)byte.MaxValue;
                colors = Utils.ReadArray<byte[], float[]>(data, cOffset, cCount, 3,
                                                          s => new[]
                                                          {
                                                              s[0] / maxByte, s[1] / maxByte, s[2] / maxByte
                                                          });
            }

            // read texcoords array
            var tOffset = offset + mdtHeader.toVertexCount + mdtHeader.uvOffset;
            var tCount = mdtHeader.uvCount;
            var maxSByte = (float)sbyte.MaxValue;
            var uvs = Utils.ReadArray<byte[], float[]>(data, tOffset, tCount, 2,
                                                       s => new[]
                                                       {
                                                           s[0] / maxSByte * uvScale[0], s[1] / maxSByte * uvScale[1]
                                                       });

            // read weights array
            var wOffset = offset + mdtHeader.toVertexCount + mdtHeader.toWeightsOffset;
            var weights = Utils.ReadArray<ushort[], float[]>(data, wOffset, vCount, 4,
                                                             s => Array.ConvertAll(s, x => (float)x / short.MaxValue));

            // read bones per vertex array
            var bOffset = offset + mdtHeader.toVertexCount + mdtHeader.toBoneIndicesOffset;
            var boneIndices = Utils.ReadArray<short[], int[]>(data, bOffset, vCount, 4,
                                                              s => Array.ConvertAll(s, x => (int)x));

            var submeshes = new List<SubMesh>();
            var triangleGroupOffset = offset + mdtHeader.vertexAttrSize;
            var lastEndIndex = 0;

            var verticesList = new List<float[]>();
            var trianglesList = new List<int>();
            var colorsList = new List<float[]>();
            var uvList = new List<float[]>();
            var weightsList = new List<float[]>();
            var bonesList = new List<int[]>();

            for (var submeshIndex = 0; submeshIndex < mdtHeader.triangleGroupCount; submeshIndex++)
            {
                var triangleGroupHeader = Utils.ReadStruct<TriangleGroupHeader>(data, ref triangleGroupOffset);
                if (triangleGroupHeader.headerSize == 32 && triangleGroupHeader.toNext == 0)
                {
                    // This is normal. Often, such blocks complete a sequence.
                    continue;
                }

                if (triangleGroupHeader.headerSize == 32 && triangleGroupHeader.toNext != 0)
                {
                    // This might be weird.
                    Log.Line($"Skip 32-bit triangle group header on {triangleGroupOffset}");
                    continue;
                }

                var submesh = new SubMesh();

                var triangleIndexOffset = triangleGroupOffset + triangleGroupHeader.headerSize;
                var triangles = ReadTriangles(triangleGroupHeader, triangleIndexOffset, data);
                var cornerVerts = GetCornerAttribute(vertices, triangles, triangleIndexOffset, "vertices");
                verticesList.AddRange(cornerVerts);
                trianglesList.AddRange(Enumerable.Range(lastEndIndex, cornerVerts.Length));

                if ((features & MeshFeatures.Weights) == MeshFeatures.Weights)
                {
                    var cornerWeights = GetCornerAttribute(weights, triangles, 0, "weights");
                    weightsList.AddRange(cornerWeights);
                    var cornerBoneIndices = GetCornerAttribute(boneIndices, triangles, 0, "vertex bones");
                    bonesList.AddRange(cornerBoneIndices);
                }

                submesh.materialIndex = BitConverter.ToInt32(data, triangleGroupOffset + 32);
                submesh.startIndex = lastEndIndex;
                submesh.indexCount = triangles.Length;
                lastEndIndex += cornerVerts.Length;
                submeshes.Add(submesh);

                if ((features & MeshFeatures.Colors) == MeshFeatures.Colors)
                {
                    var colorsTriangleOffset = BitConverter.ToInt32(data, triangleGroupOffset + 40);
                    if (colorsTriangleOffset != 0)
                    {
                        colorsTriangleOffset += triangleGroupOffset;
                        var colorTriangles = ReadTriangles(triangleGroupHeader, colorsTriangleOffset, data);
                        var cornerColors =
                            GetCornerAttribute(colors, colorTriangles, colorsTriangleOffset, "color");
                        colorsList.AddRange(cornerColors);
                    }
                }

                if ((features & MeshFeatures.UVs) == MeshFeatures.UVs &&
                    triangleGroupHeader.ReadMode != TriangleReadMode.None)
                {
                    var uvOffset = BitConverter.ToInt32(data, triangleGroupOffset + 36);
                    if (uvOffset != 0)
                    {
                        uvOffset += triangleGroupOffset;
                        var uvTriangles = ReadTriangles(triangleGroupHeader, uvOffset, data);
                        var cornerUVs = GetCornerAttribute(uvs, uvTriangles, uvOffset, "texcoords");
                        uvList.AddRange(cornerUVs);
                    }
                }

                triangleGroupOffset += triangleGroupHeader.toNext;
            }

            mesh.vertices = verticesList.ToArray();
            mesh.triangles = trianglesList.ToArray();
            mesh.color = colorsList.ToArray();
            mesh.uv = uvList.ToArray();
            mesh.weights = weightsList.ToArray();
            mesh.bones = bonesList.ToArray();
            mesh.submeshes = submeshes.ToArray();
        }

        return scene;
    }

    private static string ReadNames(MDSHeader header, byte[] data)
    {
        var offset = header.offsetToNames;
        var decodedString = Encoding.UTF8.GetString(data, offset, header.namesBlockSize);
        return decodedString.Replace("\0", " ");
    }

    private static void ReadNodes(MDSScene scene, MDSHeader header, byte[] data, string names)
    {
        scene.nodes = new MDSNode[header.nodesCount];

        var offset = header.offsetToNodes;
        for (var i = 0; i < header.nodesCount; i++)
        {
            var node = Utils.ReadStruct<Node>(data, ref offset, true);

            var type = node.meshIndex < 0 ? NodeType.Bone : NodeType.Mesh;

            MDSNode mdsNode;
            if (type == NodeType.Bone)
            {
                var boneNode = new MDSBone();
                mdsNode = boneNode;
            }
            else
            {
                mdsNode = new MDSMesh();
            }

            mdsNode.bindpose = node.bindpose;
            mdsNode.name = Utils.GetName(names, node.nameOffset);
            mdsNode.transform = node.matrix;
            mdsNode.index = i;
            mdsNode.parentIndex = node.parentIndex;
            mdsNode.type = type;
            mdsNode.meshIndex = node.meshIndex;

            scene.nodes[i] = mdsNode;
        }
    }

    private static void ReadMaterials(MDSScene scene, MDSHeader header, byte[] data, string names)
    {
        var offset = header.offsetToMaterials;

        scene.materials = new MDSMaterial[header.materialsCount];

        for (var i = 0; i < header.materialsCount; i++)
        {
            var rawMaterial = Utils.ReadStruct<Material>(data, ref offset, true);
            var mdsMaterial = new MDSMaterial();
            mdsMaterial.materialType = rawMaterial.value17;

            if (rawMaterial.value13 == 1)
            {
                mdsMaterial.name = "NONE";
                mdsMaterial.textureName = "NONE";
            }
            else
            {
                mdsMaterial.name = Utils.GetName(names, rawMaterial.nameOffset);
                mdsMaterial.textureName = Utils.GetName(names, rawMaterial.textureNameOffset);
            }

            scene.materials[i] = mdsMaterial;
        }
    }

    private static int[] ReadTriangles(TriangleGroupHeader header, int offset, byte[] data)
    {
        var triangles = new List<int>();
        if (header.ReadMode == TriangleReadMode.Triangle)
        {
            for (var i = 0; i < header.idsCount; i += 3)
            {
                var triangle = Utils.ReadArray<short, int>(data, offset, 3, 0, i => i);
                offset += 6;

                triangles.Add(triangle[0]);
                triangles.Add(triangle[1]);
                triangles.Add(triangle[2]);
            }
        }

        var readCount = 0;
        if (header.ReadMode == TriangleReadMode.TriangleStrip)
        {
            var isDefaultOrder = true;
            while (readCount < header.idsCount - 2)
            {
                var triangle = Utils.ReadArray<short, int>(data, offset, 3, 0, i => i);
                readCount++;

                triangles.Add(isDefaultOrder ? triangle[0] : triangle[1]);
                triangles.Add(isDefaultOrder ? triangle[1] : triangle[0]);
                triangles.Add(triangle[2]);
                offset += 2;

                isDefaultOrder = !isDefaultOrder;
            }
        }

        return triangles.ToArray();
    }

    private static T[][] GetCornerAttribute<T>(T[][] attribute, int[] triangles, int address, string content)
    {
        var faceAttribute = new T[triangles.Length][];
        for (var i = 0; i < triangles.Length; i++)
        {
            var triangleVertexIndex = triangles[i];
            try
            {
                faceAttribute[i] = attribute[triangleVertexIndex];
            }
            catch (Exception e)
            {
                Log.Line($"Bad index [{triangleVertexIndex}] in {content} array. Array start index: {address}, elements: {attribute.Length}");
            }
        }

        return faceAttribute;
    }
}