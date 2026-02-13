using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace dq8chr2glb.Core.InfoCfg;

public class ConfigFile
{
    public string text;

    public ConfigFile(string text)
    {
        this.text = text;
    }

    public ModelConfig ReadConfig()
    {
        var config = new ModelConfig();
        config.model = GetModelFileName();
        config.clips = GetClips();
        return config;
    }

    public string GetModelFileName()
    {
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            if (line.StartsWith("MODEL"))
            {
                var match = Regex.Match(line, @"MODEL\s+""([^""]+)""");
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    public List<Clip> GetClips()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var clips = new List<Clip>();
        var lines = text.Split('\n');
        var insideKeyBlock = false;

        foreach (var line in lines)
        {
            var t = line.Trim();

            if (t == "KEY_START;")
            {
                insideKeyBlock = true;
                continue;
            }

            if (t == "KEY_END;")
            {
                insideKeyBlock = false;
                continue;
            }

            if (!insideKeyBlock || !t.StartsWith("KEY "))
            {
                continue;
            }

            var parts = t[4..^1].Split(", ");

            clips.Add(new Clip
            {
                name = parts[0][1..^1],
                startFrame = int.Parse(parts[1]),
                endFrame = int.Parse(parts[2]),
                speed = float.Parse(parts[3])
            });
        }

        return clips;
    }
}