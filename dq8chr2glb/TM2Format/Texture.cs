using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace dq8chr2glb.TM2Format;

public class Texture
{
    public readonly string name;
    public readonly Image<Rgba32> data;

    public Texture(string name, Image<Rgba32> data)
    {
        this.name = name;
        this.data = data;
    }
}