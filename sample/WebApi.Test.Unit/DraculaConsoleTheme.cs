using Serilog.Sinks.SystemConsole.Themes;

namespace WebApi.Test.Unit;

/// <summary>
/// Dracula Console Theme
/// </summary>
public static class DraculaConsoleTheme
{
    /// <summary>
    /// 暗色模式主题，基于 Dracula 主题色
    /// </summary>
    public static AnsiConsoleTheme Dark { get; } = new(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\e[38;2;248;248;242m",                     // foreground: #F8F8F2
        [ConsoleThemeStyle.SecondaryText] = "\e[38;2;98;114;164m",             // brightBlack: #6272A4
        [ConsoleThemeStyle.TertiaryText] = "\e[38;2;68;71;90m",                // black: #21222C
        [ConsoleThemeStyle.Invalid] = "\e[38;2;241;250;140m",                  // yellow: #F1FA8C
        [ConsoleThemeStyle.Null] = "\e[38;2;98;114;164m",                      // brightBlack: #6272A4
        [ConsoleThemeStyle.Name] = "\e[38;2;189;147;249m",                     // blue: #BD93F9
        [ConsoleThemeStyle.String] = "\e[38;2;255;121;198m",                   // purple: #FF79C6
        [ConsoleThemeStyle.Number] = "\e[38;2;80;250;123m",                    // green: #50FA7B
        [ConsoleThemeStyle.Boolean] = "\e[38;2;139;233;253m",                  // cyan: #8BE9FD
        [ConsoleThemeStyle.Scalar] = "\e[38;2;80;250;123m",                    // green: #50FA7B
        [ConsoleThemeStyle.LevelVerbose] = "\e[38;2;98;114;164m",              // brightBlack: #6272A4
        [ConsoleThemeStyle.LevelDebug] = "\e[38;2;248;248;242m",               // foreground: #F8F8F2
        [ConsoleThemeStyle.LevelInformation] = "\e[38;2;255;255;255m",         // brightWhite: #FFFFFF
        [ConsoleThemeStyle.LevelWarning] = "\e[38;2;241;250;140m",             // yellow: #F1FA8C
        [ConsoleThemeStyle.LevelError] = "\e[38;2;255;85;85m",                 // red: #FF5555
        [ConsoleThemeStyle.LevelFatal] = "\e[38;2;255;85;85m\e[48;2;68;71;90m" // red: #FF5555, background: #21222C
    });

    /// <summary>
    /// 亮色模式主题，调整颜色以确保在白色背景下清晰可见
    /// </summary>
    public static AnsiConsoleTheme Light { get; } = new(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\e[38;2;40;42;54m",                           // 深色调整: 接近 black #21222C
        [ConsoleThemeStyle.SecondaryText] = "\e[38;2;98;114;164m",                // brightBlack: #6272A4
        [ConsoleThemeStyle.TertiaryText] = "\e[38;2;98;114;164m",                 // brightBlack: #6272A4
        [ConsoleThemeStyle.Invalid] = "\e[38;2;175;184;103m",                     // 较暗的 yellow: 降低亮度 #F1FA8C
        [ConsoleThemeStyle.Null] = "\e[38;2;98;114;164m",                         // brightBlack: #6272A4
        [ConsoleThemeStyle.Name] = "\e[38;2;139;108;184m",                        // 较暗的 blue: 降低亮度 #BD93F9
        [ConsoleThemeStyle.String] = "\e[38;2;188;89;146m",                       // 较暗的 purple: 降低亮度 #FF79C6
        [ConsoleThemeStyle.Number] = "\e[38;2;59;184;90m",                        // 较暗的 green: 降低亮度 #50FA7B
        [ConsoleThemeStyle.Boolean] = "\e[38;2;102;172;186m",                     // 较暗的 cyan: 降低亮度 #8BE9FD
        [ConsoleThemeStyle.Scalar] = "\e[38;2;59;184;90m",                        // 较暗的 green: 降低亮度 #50FA7B
        [ConsoleThemeStyle.LevelVerbose] = "\e[38;2;98;114;164m",                 // brightBlack: #6272A4
        [ConsoleThemeStyle.LevelDebug] = "\e[38;2;40;42;54m",                     // 深色调整: 接近 black #21222C
        [ConsoleThemeStyle.LevelInformation] = "\e[38;2;33;34;44m",               // 更深的 white: 降低亮度 #FFFFFF
        [ConsoleThemeStyle.LevelWarning] = "\e[38;2;175;184;103m",                // 较暗的 yellow: 降低亮度 #F1FA8C
        [ConsoleThemeStyle.LevelError] = "\e[38;2;188;62;62m",                    // 较暗的 red: 降低亮度 #FF5555
        [ConsoleThemeStyle.LevelFatal] = "\e[38;2;188;62;62m\e[48;2;200;200;200m" // 较暗的 red: #FF5555, 浅灰背景
    });
}