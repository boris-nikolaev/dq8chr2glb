using System;
using System.Runtime.InteropServices;
using dq8chr2glb.Core.MDSFormat;

namespace dq8chr2glb.Core;

public static class Utils
{
    public static T ReadStruct<T>(byte[] data, ref int offset, bool addToOffset = false)
    {
        var size = Marshal.SizeOf(typeof(T));
        var buffer = new byte[size];
        Array.Copy(data, offset, buffer, 0, size);
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        if (addToOffset)
        {
            offset += size;
        }

        return structure;
    }

    public static T2[] ReadArray<T, T2>(byte[] data, int start, int count, int size, Func<T, T2> convert)
    {
        var isArray = typeof(T2).IsArray;
        var result = new T2[count];
        var elementType = typeof(T).GetElementType();

        for (var i = 0; i < count; i++)
        {
            var elementOffset = start + i * (isArray ? size * Marshal.SizeOf(elementType) : Marshal.SizeOf(typeof(T)));

            if (isArray)
            {
                var array = Array.CreateInstance(elementType, size);
                Buffer.BlockCopy(data, elementOffset, array, 0, size * Marshal.SizeOf(elementType));
                result[i] = convert((T)(object)array);
            }
            else
            {
                var buffer = new byte[Marshal.SizeOf(typeof(T))];
                Array.Copy(data, elementOffset, buffer, 0, buffer.Length);

                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                var item = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                handle.Free();

                result[i] = convert(item);
            }
        }

        return result;
    }

    public static string GetName(string raw, int startIndex)
    {
        var endIndex = raw.IndexOf(' ', startIndex);

        if (endIndex == -1)
        {
            return raw[startIndex..];
        }

        return raw.Substring(startIndex, endIndex - startIndex);
    }

    public static MDSMesh GetMeshByIndex(MDSScene scene, int index)
    {
        foreach (var node in scene.nodes)
        {
            if (node.type == NodeType.Mesh)
            {
                var mesh = node as MDSMesh;
                if (mesh.meshIndex == index)
                {
                    return mesh;
                }
            }
        }

        return null;
    }

    public static MeshFeatures GetFeaturesFlags(bool vertices, bool colors, bool uvs, bool weights)
    {
        var result = MeshFeatures.None;
        if (vertices) result |= MeshFeatures.Verts;
        if (colors) result |= MeshFeatures.Colors;
        if (uvs) result |= MeshFeatures.UVs;
        if (weights) result |= MeshFeatures.Weights;
        return result;
    }
}
