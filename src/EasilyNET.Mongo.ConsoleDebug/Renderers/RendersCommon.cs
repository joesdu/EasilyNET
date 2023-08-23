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
        if (!logEvent.Properties.ContainsKey(token.PropertyName)) yield break;
        var propValue = logEvent.Properties[token.PropertyName]
                                .ToString(token.Format, formatProvider)
                                .Exec(Markup.Escape)
                                .Exec(DefaultStyle.HighlightProp);
        yield return new Markup(propValue);
    }
}