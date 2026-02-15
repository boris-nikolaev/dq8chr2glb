using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dq8chr2glb.Container;
using dq8chr2glb.Converter;
using dq8chr2glb.Converter.GLTF;
using dq8chr2glb.Core.InfoCfg;
using dq8chr2glb.Core.MDSFormat;
using dq8chr2glb.Core.MOTFormat;
using dq8chr2glb.Logger;
using SixLabors.ImageSharp;
using Texture = dq8chr2glb.TM2Format.Texture;

namespace dq8chr2glb;

public class ChrFile
{
    public bool extract;
    public bool convert;
    public bool textFormat;

    public void Process(string inputPath, string outputPath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var ctx = new Context();
        ctx.inputPath = inputPath;
        ctx.modelName = Path.GetFileNameWithoutExtension(ctx.inputPath);
        ctx.outputPath = Path.Combine(outputPath, ctx.modelName);

        var chrData = File.ReadAllBytes(ctx.inputPath);
        var container = ChrContainer.FromBytes(chrData);

        EnsurePath(ctx.outputPath);

        foreach (var file in container)
        {
            PrintTask(file);

            switch (file.extension)
            {
                case FileExtension.CFG:
                    ProcessConfig(file);
                    break;
                case FileExtension.TEXT:
                    ProcessTextFile(file);
                    break;
                case FileExtension.TM2:
                    ProcessTextures(file);
                    break;
                case FileExtension.MDS:
                    ProcessMDSFile(file);
                    break;
                case FileExtension.MOT:
                    ProcessMOTFile(file);
                    break;
                default:
                    ProcessRawFile(file);
                    break;
            }
        }

        if (convert)
        {
            foreach (var converter in Context.current.mdsConverters)
            {
                converter.Save(ctx.outputPath, textFormat);
            }
        }

        foreach (var err in Context.current.errors)
        {
            Log.Line($"{err.name}, {err.text}", LogLevel.Error);
            if (err.exception != null)
            {
                Log.Error(err.exception);
            }
        }
        
        foreach (var err in Context.current.messages)
        {
            Log.Line($"{err.name}, {err.text}");
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

    private void EnsurePath(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private void ProcessMOTFile(IncludedFile file)
    {
        if (extract)
        {
            ProcessRawFile(file);
        }

        if (convert)
        {
            foreach (var converter in Context.current.mdsConverters)
            {
                var motImporter = new Importer();
                var animation = motImporter.Import(file.data);
                converter.CreateAnimation(animation, Context.current.infoCfg);
            }
        }
    }

    private void ProcessMDSFile(IncludedFile file)
    {
        if (extract)
        {
            ProcessRawFile(file);
        }

        var mdsScene = Reader.Read(file.data, file.name);
        if (mdsScene == null)
        {
            Context.current.errors.Add(new Error(file.name, "mdsScene is empty!"));
            return;
        }

        if (convert)
        {
            var converter = new MDSConverter(file.name);
            var rootName = Path.GetFileNameWithoutExtension(file.name);
            converter.Convert(mdsScene, Context.current.textures, rootName);
            Context.current.mdsConverters.Add(converter);
        }
    }

    private void ProcessConfig(IncludedFile file)
    {
        if (extract)
        {
            var isSecond = Context.current.infoCfg != null;
            if (isSecond)
            {
                file.name = file.name.Replace(".cfg", "_2.cfg");
            }

            ProcessTextFile(file);
        }

        var data = Encoding.GetEncoding("shift_jis").GetString(file.data).TrimEnd('\0');
        var configFile = new ConfigFile(data);

        var info = configFile.ReadConfig();
        if (Context.current.infoCfg == null)
        {
            Context.current.infoCfg = info;
        }
    }

    private void ProcessTextFile(IncludedFile file)
    {
        try
        {
            var root = Path.GetDirectoryName(file.name);
            var fileName = Path.GetFileName(file.name);
            var outputPath = Path.Combine(Context.current.outputPath, root);
            var text = Encoding.GetEncoding("shift_jis").GetString(file.data).TrimEnd('\0');

            if (string.IsNullOrEmpty(text))
            {
                Context.current.errors.Add(new Error(file.name, "Text data is empty!"));
                return;
            }

            if (extract)
            {
                EnsurePath(outputPath);
                File.WriteAllText(Path.Combine(outputPath, fileName), text);
            }
        }
        catch (Exception e)
        {
            Context.current.errors.Add(new Error(file.name, "Error then process text file", e));
        }
    }

    private void ProcessRawFile(IncludedFile file)
    {
        try
        {
            var root = Path.GetDirectoryName(file.name);
            var fileName = Path.GetFileName(file.name);
            var outputPath = Path.Combine(Context.current.outputPath, root);

            if (extract)
            {
                EnsurePath(outputPath);
                File.WriteAllBytes(Path.Combine(outputPath, fileName), file.data);
            }
        }
        catch (Exception e)
        {
            Context.current.errors.Add(new Error(file.name, $"File extraction failed", e));
        }
    }

    private void ProcessTextures(IncludedFile file)
    {
        var root = Path.GetDirectoryName(file.name);
        var fileName = Path.GetFileNameWithoutExtension(file.name);
        var outputPath = Path.Combine(Context.current.outputPath, root);

        var image = TM2Format.TM2Format.GetImage(file, fileName);
        if (extract)
        {
            EnsurePath(outputPath);
            image.data.SaveAsPng(Path.Combine(outputPath, fileName + ".png"));
        }

        Context.current.textures.Add(image);
    }
}
