using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dq8chr2glb.Container;
using dq8chr2glb.Converter;
using dq8chr2glb.Core.InfoCfg;
using dq8chr2glb.Core.MDSFormat;
using dq8chr2glb.Core.MOTFormat;
using dq8chr2glb.Logger;
using SixLabors.ImageSharp;
using Texture = dq8chr2glb.TM2Format.Texture;

namespace dq8chr2glb;

public class ChrFile
{
    public ModelConfig infoCfg;
    public List<TM2Format.Texture> textures = new();
    public List<MDSConverter> mdsConverters = new();

    public bool extract;
    public bool convert;
    public bool textFormat;

    public void Process(string inputPath, string outputPath, bool isBatch = false)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var chrData = File.ReadAllBytes(inputPath);
        var container = ChrContainer.FromBytes(chrData);

        var outputName = Path.GetFileNameWithoutExtension(inputPath);
        var outputDir = Path.Combine(outputPath, outputName);
        EnsurePath(outputDir);

        foreach (var file in container)
        {
            if (!isBatch)
            {
                PrintTask(file);
            }

            switch (file.extension)
            {
                case FileExtension.CFG:
                    ProcessConfig(file, outputDir);
                    break;
                case FileExtension.TEXT:
                    ProcessTextFile(file, outputDir);
                    break;
                case FileExtension.TM2:
                    ProcessTextures(file, outputDir);
                    break;
                case FileExtension.MDS:
                    ProcessMDSFile(file, outputDir);
                    break;
                case FileExtension.MOT:
                    ProcessMOTFile(file, outputDir);
                    break;
                default:
                    ProcessRawFile(file, outputDir);
                    break;
            }
        }

        if (convert)
        {
            foreach (var converter in mdsConverters)
            {
                converter.Save(outputDir, textFormat);
            }
        }
    }

    private void PrintTask(IncludedFile file)
    {
        var spaces = 30 - file.name.Length;
        if (spaces <= 0)
        {
            spaces = 1;
        }

        var spacer = new string('.', spaces);
        Log.Line($"  Process: {file.name + spacer} {file.data.Length} bytes");
    }

    public void Clean()
    {
        infoCfg = null;
        textures = new();
        mdsConverters = new();
    }

    private void EnsurePath(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private void ProcessMOTFile(IncludedFile file, string outputDir)
    {
        if (extract)
        {
            ProcessRawFile(file, outputDir);
        }

        if (convert)
        {
            foreach (var converter in mdsConverters)
            {
                var motImporter = new Importer();
                var animation = motImporter.Import(file.data);
                converter.CreateAnimation(animation, infoCfg);
            }
        }
    }

    private void ProcessMDSFile(IncludedFile file, string outputDir)
    {
        if (extract)
        {
            ProcessRawFile(file, outputDir);
        }

        var mdsScene = Reader.Read(file.data, file.name);
        if (mdsScene == null)
        {
            return;
        }

        if (convert)
        {
            var converter = new MDSConverter(file.name);
            var rootName = Path.GetFileNameWithoutExtension(file.name);
            converter.Convert(mdsScene, textures, rootName);
            mdsConverters.Add(converter);
        }
    }

    private void ProcessConfig(IncludedFile file, string outputDir)
    {
        if (extract)
        {
            var isSecond = infoCfg != null;
            if (isSecond)
            {
                file.name = file.name.Replace(".cfg", "_2.cfg");
            }

            ProcessTextFile(file, outputDir);
        }
        
        var data = Encoding.GetEncoding("shift_jis").GetString(file.data).TrimEnd('\0');
        var configFile = new ConfigFile(data);
        
        var info = configFile.ReadConfig();
        if (infoCfg == null)
        {
            infoCfg = info;
        }
    }

    private void ProcessTextFile(IncludedFile file, string outputDir)
    {
        var root = Path.GetDirectoryName(file.name);
        var fileName = Path.GetFileName(file.name);
        var outputPath = Path.Combine(outputDir, root);
        var text = Encoding.GetEncoding("shift_jis").GetString(file.data).TrimEnd('\0');
        if (extract)
        {
            EnsurePath(outputPath);
            File.WriteAllText(Path.Combine(outputPath, fileName), text);
        }
    }

    private void ProcessRawFile(IncludedFile file, string outputDir)
    {
        var root = Path.GetDirectoryName(file.name);
        var fileName = Path.GetFileName(file.name);
        var outputPath = Path.Combine(outputDir, root);

        if (extract)
        {
            EnsurePath(outputPath);
            File.WriteAllBytes(Path.Combine(outputPath, fileName), file.data);
        }
    }

    private void ProcessTextures(IncludedFile file, string outputDir)
    {
        var root = Path.GetDirectoryName(file.name);
        var fileName = Path.GetFileNameWithoutExtension(file.name);
        var outputPath = Path.Combine(outputDir, root);

        var image = TM2Format.TM2Format.GetImage(file.data, fileName);
        if (extract)
        {
            EnsurePath(outputPath);
            image.data.SaveAsPng(Path.Combine(outputPath, fileName + ".png"));
        }

        textures.Add(image);
    }
}
