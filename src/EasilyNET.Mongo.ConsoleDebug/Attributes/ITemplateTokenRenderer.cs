using Serilog.Events;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Attributes;

/// <summary>
/// Abstract the rendering of the single log token
/// </summary>
internal interface ITemplateTokenRenderer
{
    IEnumerable<IRenderable> Render(LogEvent logEvent);
}