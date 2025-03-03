using EasilyNET.Core.Misc;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace EasilyNET.Core;

internal static class ModuleInitializer
{
    // private const string logo = """
    //                              ______          _ _       _   _ ______ _______
    //                             |  ____|        (_) |     | \ | |  ____|__   __|
    //                             | |__   __ _ ___ _| |_   _|  \| | |__     | |
    //                             |  __| / _` / __| | | | | | . ` |  __|    | |
    //                             | |___| (_| \__ \ | | |_| | |\  | |____   | |
    //                             |______\__,_|___/_|_|\__, |_| \_|______|  |_|
    //                                                   __/ |
    //                                                  |___/
    //                             """;

    private const string welcomeMessage = "Welcome EasilyNET Project";

    private static readonly string url;
    private static readonly string version;
    private static readonly string centeredWelcomeMessage;

    static ModuleInitializer()
    {
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "未知版本";
        url = Assembly.GetExecutingAssembly()
                      .GetCustomAttributes<AssemblyMetadataAttribute>()
                      .FirstOrDefault(attr => attr.Key == "RepositoryUrl")?.Value ??
              "未知URL";
        // 计算图形的宽度
        // var logoWidth = logo.Split('\n').Max(line => Encoding.ASCII.GetBytes(line).Length);
        // 计算欢迎消息前的空格数量
        //var padding = ((logoWidth - Encoding.ASCII.GetBytes(welcomeMessage).Length) / 2) - 3;
        var padding = Encoding.ASCII.GetBytes(welcomeMessage).Length / 2 - 3;
        // 创建居中的欢迎消息
        centeredWelcomeMessage = welcomeMessage.PadLeft(padding + welcomeMessage.Length);
    }

#pragma warning disable IDE0079
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
#pragma warning restore IDE0079
    internal static void Initialize()
    {
        if (TextWriterExtensions.IsAnsiSupported())
        {
            Console.WriteLine($"""
                               {centeredWelcomeMessage}
                               Ver: [32m{version}[0m
                               Url: [35m{url}[0m

                               """);
        }
        else
        {
            // Console.WriteLine(logo);
            // Console.WriteLine();
            Console.WriteLine(centeredWelcomeMessage);
            Console.Write("Ver: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(version);
            Console.ResetColor();
            Console.Write("Url: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(url);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}