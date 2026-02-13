using System;
using System.Collections.Generic;
using System.Numerics;
using dq8chr2glb.Core.MDSFormat;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace dq8chr2glb.Converter
{
    public static class UniversalMeshBuilder
    {
        public static Mesh CreateMesh(ModelRoot _root, MDSMesh mdsMesh, MDSMaterial[] materials,
                                      Dictionary<string, MaterialBuilder> materialCache)
        {
            var (hasUvs, hasSkinning) = AnalyzeMeshAttributes(mdsMesh);

            // Выбираем подходящий тип вершины на основе атрибутов
            if (hasSkinning)
            {
                if (hasUvs)
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexTexture1, VertexJoints4>(
                     _root, mdsMesh, materials, materialCache);
                }
                else
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexEmpty, VertexJoints4>(
                     _root, mdsMesh, materials, materialCache);
                }
            }
            else
            {
                if (hasUvs)
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexTexture1, VertexEmpty>(
                     _root, mdsMesh, materials, materialCache);
                }
                else
                {
                    return CreateMeshWithAllAttributes<VertexPositionNormal, VertexEmpty, VertexEmpty>(
                     _root, mdsMesh, materials, materialCache);
                }
            }
        }

        private static (bool hasUvs, bool hasSkinning) AnalyzeMeshAttributes(MDSMesh mdsMesh)
        {
            var hasUvs = mdsMesh.uv != null && mdsMesh.uv.Length > 0 &&
                          Array.Exists(mdsMesh.uv, uv => uv != null && uv.Length >= 2);
            var hasSkinning = mdsMesh.weights != null && mdsMesh.bones != null &&
                               mdsMesh.bones.Length > 0 &&
                               Array.Exists(mdsMesh.bones, b => b != null);

            return (hasUvs, hasSkinning);
        }

        private static Mesh CreateMeshWithAllAttributes<TvG, TvM, TvS>(
            ModelRoot root,
            MDSMesh mdsMesh,
            MDSMaterial[] materials,
            Dictionary<string, MaterialBuilder> materialCache)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var meshBuilder = new MeshBuilder<MaterialBuilder, TvG, TvM, TvS>(mdsMesh.name);

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var skinningData = new List<(int, float)[]>();
            var hasUvs = mdsMesh.uv != null && mdsMesh.uv.Length > 0;
            var hasSkinning = mdsMesh.weights != null && mdsMesh.bones != null;

            for (int i = 0; i < mdsMesh.vertices.Length; i++)
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

                if (hasSkinning && i < mdsMesh.bones.Length && mdsMesh.bones[i] != null)
                {
                    var boneIndices = mdsMesh.bones[i];
                    var boneWeights = mdsMesh.weights[i];

                    var bindings = new (int JointIndex, float Weight)[Math.Min(8, boneIndices.Length)];
                    for (var b = 0; b < bindings.Length; b++)
                    {
                        bindings[b] = (boneIndices[b], boneWeights[b]);
                    }

                    skinningData.Add(bindings);
                }
                else
                {
                    skinningData.Add(Array.Empty<(int, float)>());
                }
            }

            ComputeNormals(positions, mdsMesh.triangles, normals);

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

                for (var i = 0; i < indices.Count; i += 3)
                {
                    var idxA = indices[i];
                    var idxB = indices[i + 1];
                    var idxC = indices[i + 2];

                    var vertexA = CreateVertex<TvG, TvM, TvS>(
                                                              positions[idxA],
                                                              normals[idxA],
                                                              texCoords.Count > idxA ? texCoords[idxA] : Vector2.Zero,
                                                              skinningData.Count > idxA ? skinningData[idxA] : null);

                    var vertexB = CreateVertex<TvG, TvM, TvS>(
                                                              positions[idxB],
                                                              normals[idxB],
                                                              texCoords.Count > idxB ? texCoords[idxB] : Vector2.Zero,
                                                              skinningData.Count > idxB ? skinningData[idxB] : null);

                    var vertexC = CreateVertex<TvG, TvM, TvS>(
                                                              positions[idxC],
                                                              normals[idxC],
                                                              texCoords.Count > idxC ? texCoords[idxC] : Vector2.Zero,
                                                              skinningData.Count > idxC ? skinningData[idxC] : null);

                    primitive.AddTriangle(vertexA, vertexB, vertexC);
                }
            }

            return root.CreateMesh(meshBuilder);
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
                catch
                {
                }
            }

            var skinning = default(TvS);
            if (skinningData != null && typeof(TvS) != typeof(VertexEmpty))
            {
                try
                {
                    skinning.SetBindings(skinningData);
                }
                catch
                {
                }
            }

            return new VertexBuilder<TvG, TvM, TvS>(geometry, material, skinning);
        }

        private static void ComputeNormals(List<Vector3> positions, int[] triangles, List<Vector3> normals)
        {
            for (var i = 0; i < normals.Count; i++)
            {
                normals[i] = Vector3.Zero;
            }

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var idx1 = triangles[i];
                var idx2 = triangles[i + 1];
                var idx3 = triangles[i + 2];

                var p1 = positions[idx1];
                var p2 = positions[idx2];
                var p3 = positions[idx3];

                var edge1 = p2 - p1;
                var edge2 = p3 - p1;

                var normal = Vector3.Cross(edge1, edge2);
                normal = Vector3.Normalize(normal);

                normals[idx1] += normal;
                normals[idx2] += normal;
                normals[idx3] += normal;
            }

            for (var i = 0; i < normals.Count; i++)
            {
                normals[i] = Vector3.Normalize(normals[i]);
            }
        }
    }
}