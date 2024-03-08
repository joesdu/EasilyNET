namespace EasilyNET.Mongo.ConsoleDebug.Style;

internal static class DefaultStyle
{
    /// <summary>
    /// 显示属性（property）的文本.它将文本包裹在 [aqua] 标签中,以使其以浅蓝色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightProp(string text) => $"[aqua]{text}[/]";

    /// <summary>
    /// 显示“静音”（muted）文本.它将文本包裹在 [grey] 标签中,以使其以灰色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightMuted(string text) => $"[grey]{text}[/]";

    /// <summary>
    /// 是 HighlightMuted 的别名,用于突出显示详细信息.它与 HighlightMuted 使用相同的颜色
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightVerbose(string text) => HighlightMuted(text);

    /// <summary>
    /// 显示调试信息.它将文本包裹在 [silver] 标签中,以使其以银色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightDebug(string text) => $"[silver]{text}[/]";

    /// <summary>
    /// 显示信息性文本.它将文本包裹在 [deepskyblue1] 标签中,以使其以深天蓝色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightInfo(string text) => $"[deepskyblue1]{text}[/]";

    /// <summary>
    /// 显示警告信息.它将文本包裹在 [yellow] 标签中,以使其以黄色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightWarning(string text) => $"[yellow]{text}[/]";

    /// <summary>
    /// 显示错误信息.它将文本包裹在 [red] 标签中,以使其以红色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightError(string text) => $"[red]{text}[/]";

    /// <summary>
    /// 显示严重错误信息.它将文本包裹在 [maroon] 标签中,以使其以栗色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightFatal(string text) => $"[maroon]{text}[/]";

    /// <summary>
    /// 显示数字.它将文本包裹在 [lime] 标签中,以使其以酸橙色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightNumber(string text) => $"[lime]{text}[/]";

    /// <summary>
    /// 显示内容长度.它将文本包裹在 [purple] 标签中,以使其以紫色显示
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static string HighlightContentLength(string text) => $"[purple]{text}[/]";
}