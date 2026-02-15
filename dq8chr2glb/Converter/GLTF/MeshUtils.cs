using System;
using System.Collections.Generic;
using System.Numerics;
using SharpGLTF.Schema2;
using System.Runtime.InteropServices;

namespace dq8chr2glb.Converter.GLTF;

public static class MeshUtils
{
    public static void ComputeNormals(List<Vector3> positions, int[] triangles, List<Vector3> normals)
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

    public static void ApplyTransform(Node node)
    {
        Matrix4x4.Invert(node.LocalMatrix, out var invTransform);
        var normalMatrix = Matrix4x4.Transpose(invTransform);

        foreach (var primitive in node.Mesh.Primitives)
        {
            var positionAccessor = primitive.GetVertexAccessor("POSITION");
            var positions = positionAccessor.AsVector3Array();
            var newPositions = new Vector3[positions.Count];

            for (var i = 0; i < positions.Count; i++)
            {
                newPositions[i] = Vector3.Transform(positions[i], node.LocalMatrix);
            }

            var count = positionAccessor.Count;
            var format = positionAccessor.Format;

            var positionBytes = MemoryMarshal.AsBytes(newPositions.AsSpan()).ToArray();
            var newPositionBufferView = primitive.LogicalParent.LogicalParent.UseBufferView(
                 positionBytes
                );

            var newAccessor = primitive.LogicalParent.LogicalParent.CreateAccessor();
            newAccessor.SetVertexData(
                                      newPositionBufferView,
                                      0,
                                      count,
                                      format
                                     );

            primitive.SetVertexAccessor("POSITION", newAccessor);

            var normalAccessor = primitive.GetVertexAccessor("NORMAL");
            if (normalAccessor != null)
            {
                var normals = normalAccessor.AsVector3Array();
                var newNormals = new Vector3[normals.Count];

                for (var i = 0; i < normals.Count; i++)
                {
                    var transformedNormal = Vector3.TransformNormal(normals[i], normalMatrix);
                    newNormals[i] = Vector3.Normalize(transformedNormal);
                }

                var normalBytes = MemoryMarshal.AsBytes(newNormals.AsSpan()).ToArray();
                var newNormalBufferView = primitive.LogicalParent.LogicalParent.UseBufferView(
                     normalBytes
                    );

                var newNormalAccessor = primitive.LogicalParent.LogicalParent.CreateAccessor();
                newNormalAccessor.SetVertexData(
                                                newNormalBufferView,
                                                0,
                                                normalAccessor.Count,
                                                normalAccessor.Format
                                               );

                primitive.SetVertexAccessor("NORMAL", newNormalAccessor);
            }
        }

        node.LocalMatrix = Matrix4x4.Identity;
    }
}
