using EasilyNET.Mongo.ConsoleDebug.Attributes;
using Serilog.Events;
using Serilog.Parsing;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal sealed class EventPropertyTokenRenderer(PropertyToken token) : ITemplateTokenRenderer
{
    public IEnumerable<IRenderable> Render(LogEvent logEvent) => RendersCommon.RenderProperty(logEvent, token);
}