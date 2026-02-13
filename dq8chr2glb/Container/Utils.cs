using System.Collections.Generic;

namespace dq8chr2glb.Container;

public static class Utils
{
    public static List<IncludedFile> SortFiles(List<IncludedFile> files)
    {
        var other = new List<IncludedFile>();
        var configs = new List<IncludedFile>();
        var textures = new List<IncludedFile>();
        var models = new List<IncludedFile>();
        var motion = new List<IncludedFile>();

        foreach (var file in files)
        {
            switch (file.extension)
            {
                case FileExtension.TEXT:
                    configs.Add(file);
                    break;
                case FileExtension.TM2:
                    textures.Add(file);
                    break;
                case FileExtension.MDS:
                    models.Add(file);
                    break;
                case FileExtension.MOT:
                    motion.Add(file);
                    break;
                case FileExtension.CHR:
                default:
                    other.Add(file);
                    break;
            }
        }

        var output = new List<IncludedFile>();
        output.AddRange(other);
        output.AddRange(configs);
        output.AddRange(textures);
        output.AddRange(models);
        output.AddRange(motion);

        return output;
    }
}