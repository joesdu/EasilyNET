using EasilyNET.Mongo.ConsoleDebug.Attributes;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal sealed class ExceptionTokenRenderer : ITemplateTokenRenderer
{
    public IEnumerable<IRenderable> Render(LogEvent logEvent)
    {
        if (logEvent.Exception is not null)
        {
            yield return logEvent.Exception.GetRenderable();
        }
    }
}