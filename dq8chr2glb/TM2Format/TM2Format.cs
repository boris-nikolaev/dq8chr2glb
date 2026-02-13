using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace dq8chr2glb.TM2Format;

// Взято из https://github.com/Souzooka/TM2Unswizzler
public static class TM2Format
{
    public static Texture GetImage(byte[] data, string name)
    {
        var header = TM2Header.GetHeader(data);
        var image = GetImageBuffer(header, data);

        var img = new Image<Rgba32>(header.Width, header.Height);

        for (var y = 0; y < header.Height; y++)
        {
            for (var x = 0; x < header.Width; x++)
            {
                var i = (y * header.Width + x) * 4;
                var r = image[i + 2];
                var g = image[i + 1];
                var b = image[i + 0];
                var a = image[i + 3];

                img[x, y] = new Rgba32(r, g, b, a);
            }
        }

        return new Texture(name, img);
    }

    private static int GetAddress8BppSwizzle(int width, int x, int y)
    {
        var block = (y & (~0x0f)) * width + (x & (~0x0f)) * 2;
        var swap = (((y + 2) >> 2) & 0x01) * 4;
        var line = (((y & (~0x03)) >> 1) + (y & 0x01)) & 0x07;
        var column = line * width * 2 + ((x + swap) & 0x07) * 4;
        var offset = ((y >> 1) & 0x01) + ((x >> 2) & 0x02);
        return block + column + offset;
    }

    private static byte[][] GetPalette8bbp(byte[] file, TM2Header header)
    {
        var palette = new byte[256][];

        for (var i = 0; i < 256; ++i)
        {
            var rgba = new byte[4];
            Array.Copy(file, 0x40 + header.ImageSize + i * 4, rgba, 0, 4);
            rgba[3] = (byte)Math.Min(rgba[3] << 1, 0xFF);

            (rgba[2], rgba[0]) = (rgba[0], rgba[2]);

            palette[(i & 0xe7) | ((i & 0x10) >> 1) | ((i & 0x08) << 1)] = rgba;
        }

        return palette;
    }

    private static byte[] GetImageBuffer(TM2Header header, byte[] tm2file)
    {
        var palette = GetPalette8bbp(tm2file, header);

        var imageBuf = new byte[4 * header.Width * header.Height];
        for (var y = header.Height - 1; y >= 0; --y)
        {
            for (var x = 0; x < header.Width; ++x)
            {
                var c = tm2file[0x40 + GetAddress8BppSwizzle(header.Width, x, y)];
                Array.Copy(palette[c], 0, imageBuf, (x * 4) + (header.Width * y) * 4, 4);
            }
        }

        return imageBuf;
    }
}