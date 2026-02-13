using System.Runtime.InteropServices;

namespace dq8chr2glb.Container;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct ImgFile
{
    [FieldOffset(0x00)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] Name;

    [FieldOffset(0x14)] public uint FileTypeFlag;
    [FieldOffset(0x20)] public uint HeaderSize;
    [FieldOffset(0x24)] public uint DataOffset;
    [FieldOffset(0x34)] public uint FileSize;
    [FieldOffset(0x3C)] public uint Padding;
}