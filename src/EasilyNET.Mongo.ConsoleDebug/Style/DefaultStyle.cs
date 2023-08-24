namespace EasilyNET.Mongo.ConsoleDebug.Style;

internal static class DefaultStyle
{
    internal static string HighlightProp(string text) => $"[lime]{text}[/]";

    internal static string HighlightMuted(string text) => $"[grey]{text}[/]";

    internal static string HighlightVerbose(string text) => HighlightMuted(text);

    internal static string HighlightDebug(string text) => $"[silver]{text}[/]";

    internal static string HighlightInfo(string text) => $"[deepskyblue1]{text}[/]";

    internal static string HighlightWarning(string text) => $"[yellow]{text}[/]";

    internal static string HighlightError(string text) => $"[red]{text}[/]";

    internal static string HighlightFatal(string text) => $"[maroon]{text}[/]";
}