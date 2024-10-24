using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace EasilyNET.Core;

internal static class ModuleInitializer
{
    private const string url = "https://github.com/joesdu/EasilyNET";

    private const string logo = """
                                 ________                 _   __          ____  _____  ________  _________  
                                |_   __  |               (_) [  |        |_   \|_   _||_   __  ||  _   _  | 
                                  | |_ \_| ,--.   .--.   __   | |   _   __ |   \ | |    | |_ \_||_/ | | \_| 
                                  |  _| _ `'_\ : ( (`\] [  |  | |  [ \ [  ]| |\ \| |    |  _| _     | |     
                                 _| |__/ |// | |, `'.'.  | |  | |   \ '/ /_| |_\   |_  _| |__/ |   _| |_    
                                |________|\'-;__/[\__) )[___][___][\_:  /|_____|\____||________|  |_____|   
                                                                   \__.'                                    
                                """;

    private const string welcomeMessage = "Welcome EasilyNET Project";

    private static bool _initialized;
    private static readonly Lock _lock = new();

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        // 使用锁来确保线程安全
        lock (_lock)
        {
            if (_initialized) return;
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "未知版本";
            // 计算图形的宽度
            var logoWidth = logo.Split('\n').Max(line => Encoding.ASCII.GetBytes(line).Length);
            // 计算欢迎消息前的空格数量
            var padding = ((logoWidth - Encoding.ASCII.GetBytes(welcomeMessage).Length) / 2) - 3;
            // 创建居中的欢迎消息
            var centeredWelcomeMessage = welcomeMessage.PadLeft(padding + welcomeMessage.Length);
            Console.WriteLine($"""
                               {logo}

                               {centeredWelcomeMessage}
                               Ver: [32m{version}[0m
                               Url: [34m{url}[0m

                               """);
            // 标记为已初始化
            _initialized = true;
        }
    }
}