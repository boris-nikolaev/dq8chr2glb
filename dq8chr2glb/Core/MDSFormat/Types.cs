using System.Runtime.InteropServices;

namespace dq8chr2glb.Core.MDSFormat;

public class MDSScene
{
    public MDSNode[] nodes;
    public MDSMaterial[] materials;
}

public class MDSNode
{
    public string name;
    public int index;
    public int parentIndex;
    public MDSMatrix transform;
    public MDSMatrix bindpose;
    public NodeType type;
    public int meshIndex;
}

public class MDSBone : MDSNode
{
}

public class MDSMesh : MDSNode
{
    public SubMesh[] submeshes;
    public MeshFeatures features;
    public AABB bounds;
    // merged data
    public float[][] vertices;
    public float[][] color;
    public float[][] uv;
    public float[][] weights;
    public int[][] bones;
    public int[] triangles;
}

public class SubMesh
{
    public int materialIndex;
    public int startIndex;
    public int indexCount;
}

public class MDSMaterial
{
    public string name;
    public string textureName;
    public int materialType;
}

[StructLayout(LayoutKind.Explicit, Size=64)]
public struct MDSMatrix
{
    [FieldOffset(00)]public float m00;
    [FieldOffset(04)]public float m01;
    [FieldOffset(08)]public float m02;
    [FieldOffset(12)]public float m03;
    [FieldOffset(16)]public float m10;
    [FieldOffset(20)]public float m11;
    [FieldOffset(24)]public float m12;
    [FieldOffset(28)]public float m13;
    [FieldOffset(32)]public float m20;
    [FieldOffset(36)]public float m21;
    [FieldOffset(40)]public float m22;
    [FieldOffset(44)]public float m23;
    [FieldOffset(48)]public float m30;
    [FieldOffset(52)]public float m31;
    [FieldOffset(56)]public float m32;
    [FieldOffset(60)]public float m33;

    public float[] elements => new[]
    {
        m00, m01, m02, m03,
        m10, m11, m12, m13,
        m20, m21, m22, m23,
        m30, m31, m32, m33
    };
}