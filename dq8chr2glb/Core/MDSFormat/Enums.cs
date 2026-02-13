using System;

namespace dq8chr2glb.Core.MDSFormat;

public enum TriangleReadMode : short
{
    None = 0,
    Triangle = 3,
    TriangleStrip = 4,
    Unknown1 = 17,
    Unknown2 = 19,
}

public enum NodeType
{
    None,
    Mesh,
    Bone
}

[Flags]
public enum MeshFeatures
{
    None = 0,
    Verts = 1 << 0,
    Colors = 1 << 1,
    UVs = 1 << 2,
    Weights = 1 << 3,
}
