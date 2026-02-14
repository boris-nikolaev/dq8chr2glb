using System;

namespace dq8chr2glb.Logger;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    None
}

public static class Log
{
    public static void Line(object data, LogLevel level = LogLevel.Debug)
    {
        Console.WriteLine(data.ToString());
    }
    
    public static void Error(Exception e, LogLevel level = LogLevel.Error)
    {
        Console.WriteLine($"{e.Message}\n From:\n{e.Source}\nStacktrace:\n{e.StackTrace}");
    }
}
