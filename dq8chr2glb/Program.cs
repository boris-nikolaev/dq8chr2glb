using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using dq8chr2glb.Converter;
using dq8chr2glb.Logger;

namespace dq8chr2glb;

public class Program
{
    public static void Main(string[] args)
    {
        var rootCommand = new RootCommand("Converts .CHR files from Dragon Quest VIII (Playstation 2) to .glb/.glTF format.")
        {
            new Option<string>("-i", "--input") { Description = "Input path", HelpName = "input path"},
            new Option<string>("-o", "--output") { Description = "Output path", HelpName = "output path"},
            new Option<OutputFormat>("-f", "--format") {Description = "Output format", DefaultValueFactory = _ => OutputFormat.GLB},
            new Option<bool>("-e", "--extract") {Description = "Extract only - unpack .chr without conversion"},
            new Option<bool>("-b", "--batch") {Description = "Batch mode - process all .chr files in the input directory"},
            new Option<LogMode>("-l", "--log") { Description = "Log level", DefaultValueFactory = _ => LogMode.MINIMAL },
        };

        var parser = rootCommand.Parse(args);

        var inputPath = parser.GetValue<string>("-i");
        var outputPath = parser.GetValue<string>("-o");
        var extractOnly = parser.GetValue<bool>("-e");
        var textFormat = parser.GetValue<OutputFormat>("-f") == OutputFormat.GLTF;
        var batchMode = parser.GetValue<bool>("-b");

        var chrFile = new ChrFile();
        chrFile.convert = !extractOnly;
        chrFile.extract = extractOnly;
        chrFile.textFormat = textFormat;

        if (string.IsNullOrEmpty(inputPath))
        {
            Console.WriteLine("Input path is required! Use -h to see help screen.");
            return;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            var dirName = Path.GetFileNameWithoutExtension(inputPath);
            outputPath = Path.Combine(Path.GetDirectoryName(inputPath), dirName);
        }

        if (batchMode)
        {
            var files = Directory.GetFiles(inputPath, "*.chr", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                Log.Line($"Processing: {Path.GetFileName(file)}", LogLevel.Info);
                try
                {
                    chrFile.Process(file, outputPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            Console.WriteLine(files.Length == 0 ? "No .chr files found in the input directory." : "Done!");
        }
        else
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Input file not found: {inputPath}");
                return;
            }

            chrFile.Process(inputPath, outputPath);
            Console.WriteLine("Done!");
        }
    }
}
