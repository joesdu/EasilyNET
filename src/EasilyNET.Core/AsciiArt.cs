using System.Text;

namespace EasilyNET.Core;

/// <summary>
/// 一些有趣的 ASCII 艺术图案
/// </summary>
public static class AsciiArt
{
    /// <summary>
    /// EasilyNET Logo
    /// </summary>
    public const string Logo = """
                                ______          _ _       _   _ ______ _______
                               |  ____|        (_) |     | \ | |  ____|__   __|
                               | |__   __ _ ___ _| |_   _|  \| | |__     | |
                               |  __| / _` / __| | | | | | . ` |  __|    | |
                               | |___| (_| \__ \ | | |_| | |\  | |____   | |
                               |______\__,_|___/_|_|\__, |_| \_|______|  |_|
                                                     __/ |
                                                    |___/ 
                               """;

    /// <summary>
    /// 猫咪
    /// </summary>
    public const string Cat = """
                                  |\__/,|   (`\
                                _.|o o  |_   ) )
                              -(((---(((--------
                              """;

    /// <summary>
    /// </summary>
    /// <param name="logo"></param>
    /// <returns></returns>
    public static int GetLogoWidth(this string logo) => logo.Split('\n').Max(line => Encoding.ASCII.GetBytes(line).Length);
}