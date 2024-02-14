using EasilyNET.Mongo.ConsoleDebug.Attributes;
using EasilyNET.Mongo.ConsoleDebug.Extensions;
using EasilyNET.Mongo.ConsoleDebug.Style;
using Serilog.Events;
using Serilog.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal sealed class PropertyTemplateRenderer(PropertyToken token) : ITemplateTokenRenderer
{
    public IEnumerable<IRenderable> Render(LogEvent logEvent)
    {
        var value = new StructureValue(logEvent.Properties.Select(p => new LogEventProperty(p.Key, p.Value)));
        var propValue = value.ToString(token.Format, null).Exec(Markup.Escape).Exec(DefaultStyle.HighlightMuted);
        yield return new Markup(propValue);
    }
}
