using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace dq8chr2glb.Container;

public class ChrContainer
{
    public static List<IncludedFile> FromBytes(byte[] chrFile)
    {
        var result = new List<IncludedFile>();
        var pos = 0;

        while (pos < chrFile.Length)
        {
            if (pos + 80 > chrFile.Length)
            {
                break;
            }

            var buffer = chrFile.AsSpan(pos, 80);
            var dataSize = BitConverter.ToUInt32(buffer.Slice(68, 4));
            var nextOffset = BitConverter.ToUInt32(buffer.Slice(72, 4));
            var name = ExtractName(buffer.Slice(0, 64).ToArray());

            if (pos + 80 + dataSize > chrFile.Length)
            {
                break;
            }

            var data = chrFile.AsSpan(pos + 80, (int)dataSize).ToArray();

            if (dataSize < 3)
            {
                break;
            }

            var subContainers = new List<IncludedFile>();

            if (Encoding.ASCII.GetString(data, 0, 3) == "IM3")
            {
                var files = UnpackImgContainer(name, data);
                subContainers.AddRange(files);
            }
            else if (name.EndsWith(".chr"))
            {
                var files = FromBytes(data);
                subContainers.AddRange(files);
            }
            else
            {
                var file = new IncludedFile();
                file.name = name;
                file.data = data;
                subContainers.Add(file);
            }

            foreach (var file in subContainers)
            {
                var extension = file.name.ToLower().Split(".")[^1];
                file.extension = extension switch
                {
                    "mds" => FileExtension.MDS,
                    "mot" => FileExtension.MOT,
                    "tm2" => FileExtension.TM2,
                    "cfg" => FileExtension.TEXT,
                    "img" => FileExtension.IMG,
                    _     => FileExtension.TEXT
                };

                if (file.name == "info.cfg")
                {
                    file.extension = FileExtension.CFG;
                }
            }

            result.AddRange(subContainers);
            pos += (int)nextOffset;
        }

        result = Utils.SortFiles(result);
        return result;
    }

    private static List<IncludedFile> UnpackImgContainer(string root, byte[] imgData)
    {
        var rootFolderName = Path.GetFileNameWithoutExtension(root) + "_img";

        var fileCount = BitConverter.ToUInt32(imgData, 8);
        var currentOffset = 16L;

        var files = new List<IncludedFile>();
        for (var i = 0; i < fileCount; i++)
        {
            var file = new IncludedFile();

            var headerBytes = new byte[64];
            Array.Copy(imgData, currentOffset, headerBytes, 0, 64);

            var handle = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            var imgFile = Marshal.PtrToStructure<ImgFile>(handle.AddrOfPinnedObject());
            handle.Free();

            var name = ExtractName(imgFile.Name);
            var isTexture = !name.StartsWith("#");
            name = name.Replace("#", "");

            name = Path.Combine(rootFolderName, name);

            file.extension = isTexture ? FileExtension.TM2 : FileExtension.TEXT;
            file.name = name + (isTexture ? ".tm2" : ".cfg");

            var fileData = new byte[imgFile.FileSize];
            Array.Copy(imgData, imgFile.DataOffset, fileData, 0, imgFile.FileSize);

            file.data = fileData;

            files.Add(file);

            currentOffset += 64;
        }

        return files;
    }

    private static string ExtractName(byte[] nameBytes)
    {
        var len = Array.IndexOf(nameBytes, (byte)0);
        if (len < 0)
        {
            len = 16;
        }

        return Encoding.ASCII.GetString(nameBytes, 0, len).Trim();
    }
}