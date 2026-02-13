using System;
using System.Collections.Generic;
using System.IO;

namespace dq8chr2glb;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            ShowHelp();
            return;
        }

        var extractOnly = false;
        var textFormat = false;
        var batchMode = false;

        var processedArgs = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "-e")
                extractOnly = true;
            else if (arg == "-t")
                textFormat = true;
            else if (arg == "-b")
                batchMode = true;
            else
                processedArgs.Add(arg);
        }

        if (batchMode)
        {
            if (processedArgs.Count != 1)
            {
                Console.WriteLine("Error: Batch mode (-b) requires exactly one argument: <input_dir>");
                ShowHelp();
                return;
            }
        }
        else
        {
            if (processedArgs.Count != 2)
            {
                Console.WriteLine("Error: Expected <input_file> and <output_dir>");
                ShowHelp();
                return;
            }
        }

        var inputPath = processedArgs[0];
        var outputPath = batchMode ? inputPath : processedArgs[1];

        var chrFile = new ChrFile();
        chrFile.convert = !extractOnly;
        chrFile.extract = extractOnly;
        chrFile.textFormat = textFormat;

        if (batchMode)
        {
            var files = Directory.GetFiles(inputPath, "*.chr", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                Console.WriteLine($"Processing: {Path.GetFileName(file)}");
                try
                {
                    chrFile.Process(file, outputPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                chrFile.Clean();
            }

            Console.WriteLine(files.Length == 0 ? "No .chr files found in the input directory." : "Done!");
        }
        else
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: Input file not found: {inputPath}");
                return;
            }

            chrFile.Process(inputPath, outputPath);
            Console.WriteLine("Done!");
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Converts .CHR files from Dragon Quest VIII (Playstation 2) to .glb/.glTF format.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("    dq8chr2glb.exe <input_file> <output_dir> [options]");
        Console.WriteLine("    dq8chr2glb.exe <input_dir> -b (batch mode: output files are saved in the <input_dir>)");
        Console.WriteLine("Examples:");
        Console.WriteLine("    dq8chr2glb.exe \"C:\\Users\\Boris\\Desktop\\ChrFormatTest\\ap002.chr\" \"C:\\Users\\Boris\\Desktop\\ChrFormatTest\" -e");
        Console.WriteLine("    dq8chr2glb.exe \"C:\\Users\\Boris\\Desktop\\ChrFormatTest\" -b");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("   -e - Extract only - unpack .chr without conversion");
        Console.WriteLine("   -t - Output as .glTF (text) instead of .glb (binary)");
        Console.WriteLine("   -b - Batch mode - process all .chr files in the input directory");
        Console.WriteLine();
        var line = Console.ReadLine();
    }
}