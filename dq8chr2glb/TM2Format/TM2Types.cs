using System;
using System.Runtime.InteropServices;

namespace dq8chr2glb.TM2Format;

public struct TM2Header
{
    public int Label;
    public byte Version;
    public byte Format;
    public int padding0;
    public int padding1;
    public uint TotalSize;
    public uint ClutSize;
    public uint ImageSize;
    public ushort HeaderSize;
    public ushort ClutColors;
    public TM2ImageFormat TM2ImageFormat;
    public byte MipMapTextures;
    public ClutType ClutType;
    ImageType ImageType;
    public ushort Width;
    public ushort Height;
    public ulong GsTex0;
    public ulong GsText1;
    public uint GsReg;
    public uint GsTexClut;

    public static TM2Header GetHeader(byte[] file)
    {
        var headerBuf = new byte[Marshal.SizeOf<TM2Header>()];
        Array.Copy(file, headerBuf, headerBuf.Length);
        var handle = GCHandle.Alloc(headerBuf, GCHandleType.Pinned);
        var header = (TM2Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(TM2Header));
        handle.Free();
        return header;
    }
}

public enum TM2ImageFormat : byte
{
    Default = 0,
    Format16bpp = 1,
    Format24bpp = 2,
    Format32bpp = 3,
    Format4bbp = 4,
    Format8bbp = 5
}

public enum ImageType : byte
{
    unk0 = 0
}

public enum ClutType : byte
{
    unk0 = 0
}
