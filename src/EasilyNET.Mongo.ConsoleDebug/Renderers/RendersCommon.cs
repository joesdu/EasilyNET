using EasilyNET.Mongo.ConsoleDebug.Extensions;
using EasilyNET.Mongo.ConsoleDebug.Style;
using Serilog.Events;
using Serilog.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal static class RendersCommon
{
    internal static IEnumerable<IRenderable> RenderProperty(LogEvent logEvent, PropertyToken token, IFormatProvider? formatProvider = null)
    {
        if (!logEvent.Properties.TryGetValue(token.PropertyName, out var property)) yield break;
        var propValue = property
                        .ToString(token.Format, formatProvider)
                        .TrimStart('\"').TrimEnd('\"')
                        .Replace("\\\"", "\"")
                        .Exec(Markup.Escape);
        propValue = token.PropertyName switch
        {
            "StatusCode" or "ElapsedMilliseconds" => propValue.Exec(DefaultStyle.HighlightNumber),
            "ContentLength"                       => propValue.Exec(DefaultStyle.HighlightContentLength),
            _                                     => propValue.Exec(DefaultStyle.HighlightProp)
        };
        yield return new Markup(propValue);
    }
}
