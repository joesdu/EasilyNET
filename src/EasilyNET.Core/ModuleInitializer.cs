using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using EasilyNET.Core.Misc;

namespace EasilyNET.Core;

internal static class ModuleInitializer
{
    private const string url = "https://github.com/joesdu/EasilyNET";

    private const string welcomeMessage = "Welcome EasilyNET Project";

    private static bool _initialized;
    private static readonly Lock _lock = new();

#pragma warning disable IDE0079
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
#pragma warning restore IDE0079
    internal static void Initialize()
    {
        // 使用锁来确保线程安全
        lock (_lock)
        {
            if (_initialized) return;
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "未知版本";
            // 计算图形的宽度
            var logoWidth = AsciiArt.Logo.GetLogoWidth();
            // 计算欢迎消息前的空格数量
            var padding = ((logoWidth - Encoding.ASCII.GetBytes(welcomeMessage).Length) / 2) - 3;
            // 创建居中的欢迎消息
            var centeredWelcomeMessage = welcomeMessage.PadLeft(padding + welcomeMessage.Length);
            if (TextWriterExtensions.IsAnsiSupported())
            {
                Console.WriteLine($"""
                                   {AsciiArt.Logo}

                                   {centeredWelcomeMessage}
                                   Ver: [32m{version}[0m
                                   Url: [35m{url}[0m

                                   """);
            }
            else
            {
                Console.WriteLine(AsciiArt.Logo);
                Console.WriteLine();
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

            // 标记为已初始化
            _initialized = true;
        }
    }
}