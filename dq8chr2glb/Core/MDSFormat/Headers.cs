using System.Runtime.InteropServices;

namespace dq8chr2glb.Core.MDSFormat;

[StructLayout(LayoutKind.Explicit, Size = 80)]
public struct MDSHeader
{
    [FieldOffset(00)] public int MagicCode;
    [FieldOffset(04)] public int version;
    [FieldOffset(08)] public int headerSize;
    [FieldOffset(12)] public int nodesCount;
    [FieldOffset(20)] public int nodeSize;
    [FieldOffset(24)] public int offsetToNodes;
    [FieldOffset(32)] public int meshCount;
    [FieldOffset(36)] public int offsetToMDT;
    [FieldOffset(44)] public int materialsCount;
    [FieldOffset(48)] public int materialSize;
    [FieldOffset(52)] public int offsetToMaterials;
    [FieldOffset(56)] public int offsetToNames;
    [FieldOffset(60)] public int namesBlockSize;
}

[StructLayout(LayoutKind.Explicit, Size = 256)]
public struct MDTHeader
{
    [FieldOffset(00)] public int MagicCode;
    [FieldOffset(08)] public int toVertexCount;
    [FieldOffset(12)] public int totalSize;

    [FieldOffset(16)] public int value1; // always 2064?
    [FieldOffset(20)] public int value2; // always 4104?
    [FieldOffset(24)] public int value3; // always 4112?

    [FieldOffset(32)] public float scaleX;
    [FieldOffset(36)] public float scaleY;

    [FieldOffset(40)] public float scaleZ;

    // [FieldOffset(64)] public Float3 someScale; // always 1.079?
    [FieldOffset(80)] public float uvScaleX;
    [FieldOffset(84)] public float uvScaleY;

    [FieldOffset(96)] public int value4; // always 144?

    [FieldOffset(112)] public int triangleGroupCount;
    [FieldOffset(116)] public int vertexAttrSize;

    [FieldOffset(128)] public int value5; // some offset

    [FieldOffset(144)] public int vertsCount;
    [FieldOffset(148)] public int bytesToFirstVertex;
    [FieldOffset(152)] public int colorsCount;
    [FieldOffset(156)] public int bytesToFirstColor;
    [FieldOffset(160)] public int uvCount;
    [FieldOffset(164)] public int uvOffset;
    [FieldOffset(176)] public int bonesPerVertex;
    [FieldOffset(180)] public int toWeightsOffset;
    [FieldOffset(184)] public int toBoneIndicesOffset;

    [FieldOffset(188)] public int value6; // always 1?
    [FieldOffset(192)] public AABB bounds;
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct AABB
{
    [FieldOffset(00)] public float minX;
    [FieldOffset(04)] public float minY;
    [FieldOffset(08)] public float minZ;
    [FieldOffset(12)] public int value1; // corner flag?
    [FieldOffset(16)] public float maxX;
    [FieldOffset(20)] public float maxY;
    [FieldOffset(24)] public float maxZ;
    [FieldOffset(28)] public int value2; // corner flag?
    [FieldOffset(32)] public float centerX;
    [FieldOffset(36)] public float centerY;
    [FieldOffset(40)] public float centerZ;
    [FieldOffset(44)] public float floatValue1; // ?
}

[StructLayout(LayoutKind.Explicit, Size = 160)]
public struct Node
{
    [FieldOffset(00)] public int nameOffset;
    [FieldOffset(12)] public int meshIndex;
    [FieldOffset(16)] public int parentIndex;
    [FieldOffset(32)] public MDSMatrix matrix;
    [FieldOffset(96)] public MDSMatrix bindpose;
}

[StructLayout(LayoutKind.Explicit, Size = 96)]
public struct Material
{
    [FieldOffset(00)] public float value1;
    [FieldOffset(04)] public float value2;
    [FieldOffset(08)] public float value3;
    [FieldOffset(12)] public float value4;

    [FieldOffset(16)] public float value5;
    [FieldOffset(20)] public float value6;
    [FieldOffset(24)] public float value7;
    [FieldOffset(28)] public float value8;

    [FieldOffset(32)] public float value9;
    [FieldOffset(36)] public float value10;
    [FieldOffset(40)] public float value11;
    [FieldOffset(44)] public float value12;

    [FieldOffset(48)] public int nameOffset;
    [FieldOffset(52)] public int textureNameOffset;

    [FieldOffset(64)] public int value13; // 0 для обычных материалов, 1 для материалов без имён
    [FieldOffset(68)] public int value14;
    [FieldOffset(72)] public int value15;
    [FieldOffset(76)] public int value16;

    [FieldOffset(80)] public int value17;
    [FieldOffset(84)] public int value18;
    [FieldOffset(88)] public int value19;
    [FieldOffset(92)] public int value20;
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct TriangleGroupHeader
{
    [FieldOffset(00)] public short toNext;
    [FieldOffset(04)] public short readMode;

    [FieldOffset(06)] public short idsCount;

    // [FieldOffset(06)]public short dataSize;
    [FieldOffset(16)] public short headerSize;

    public TriangleReadMode ReadMode => (TriangleReadMode)readMode;
}
