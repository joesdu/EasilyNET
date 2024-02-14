using EasilyNET.Mongo.ConsoleDebug.Attributes;
using Serilog.Events;
using Serilog.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal sealed class TimestampTokenRenderer(PropertyToken token) : ITemplateTokenRenderer
{
    public IEnumerable<IRenderable> Render(LogEvent logEvent)
    {
        yield return new Text(logEvent.Timestamp.ToString(token.Format), Spectre.Console.Style.Plain);
    }
}
