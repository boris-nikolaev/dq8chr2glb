using System;
using System.Collections.Generic;
using dq8chr2glb.Converter.GLTF;
using dq8chr2glb.Core.InfoCfg;

namespace dq8chr2glb.Converter;

public class Message
{
    public readonly string name;
    public readonly string text;
    
    public Message(string name, string text)
    {
        this.name = name;
        this.text = text;
    }
}

public class Error
{
    public readonly string name;
    public readonly string text;
    public readonly Exception? exception;

    public Error(string name, string text, Exception? exception = null)
    {
        this.name = name;
        this.text = text;
        this.exception = exception;
    }
}

public class Context
{
    public static Context current;
    
    public string modelName;
    public string inputPath;
    public string outputPath;
    public ModelConfig infoCfg;
    public List<TM2Format.Texture> textures = new();
    public List<MDSConverter> mdsConverters = new();
    public List<Error> errors = new();
    public List<Message> messages = new();

    public Context()
    {
        current = this;
    }
}
