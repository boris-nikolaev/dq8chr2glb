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
    // public const LogLevel LogLevel = Logger.LogLevel.Info;
    
    public static void Line(object data, LogLevel level = LogLevel.Debug)
    {
        var lastColor = Console.ForegroundColor;
        var color = level switch
        {
            LogLevel.Error   => ConsoleColor.Red,
            LogLevel.Debug   => ConsoleColor.White,
            LogLevel.Info    => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.None    => ConsoleColor.White,
        };

        if (level == LogLevel.Debug)
        {
            return;
        }

        Console.ForegroundColor = color;
        Console.WriteLine(data.ToString());
        Console.ForegroundColor = lastColor;
    }

    public static void Error(Exception e, LogLevel level = LogLevel.Error)
    {
        var lastColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"[{level}] ");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        // Console.Write($"{e.Message}\n From:\n{e.Source}\nStacktrace:\n{e.StackTrace}\n");
        Console.Write($"{e.Message}\n");
        Console.ForegroundColor = lastColor;
    }
}
