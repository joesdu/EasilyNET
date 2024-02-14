using EasilyNET.Mongo.ConsoleDebug.Attributes;
using EasilyNET.Mongo.ConsoleDebug.Style;
using Serilog.Events;
using Serilog.Parsing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal sealed class LevelTokenRenderer(PropertyToken token) : ITemplateTokenRenderer
{
    public IEnumerable<IRenderable> Render(LogEvent logEvent)
    {
        var formatMoniker = GetFormatMoniker(logEvent);
        yield return new Markup(formatMoniker);
    }

    private string GetFormatMoniker(LogEvent logEvent)
    {
        var levelMoniker = LevelOutputFormat.GetLevelMoniker(logEvent.Level, token.Format);
        return logEvent.Level switch
        {
            LogEventLevel.Verbose     => DefaultStyle.HighlightVerbose(levelMoniker),
            LogEventLevel.Debug       => DefaultStyle.HighlightDebug(levelMoniker),
            LogEventLevel.Information => DefaultStyle.HighlightInfo(levelMoniker),
            LogEventLevel.Warning     => DefaultStyle.HighlightWarning(levelMoniker),
            LogEventLevel.Error       => DefaultStyle.HighlightError(levelMoniker),
            LogEventLevel.Fatal       => DefaultStyle.HighlightFatal(levelMoniker),
            _                         => levelMoniker
        };
    }
}
