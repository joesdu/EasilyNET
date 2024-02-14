using EasilyNET.Mongo.ConsoleDebug.Attributes;
using EasilyNET.Mongo.ConsoleDebug.Extensions;
using EasilyNET.Mongo.ConsoleDebug.Renderers;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Spectre.Console;

namespace EasilyNET.Mongo.ConsoleDebug;

internal sealed class SpectreConsoleSink(string outputTemplate) : ILogEventSink
{
    private readonly ITemplateTokenRenderer[] renderers = InitializeRenders(outputTemplate).ToArray();

    public void Emit(LogEvent logEvent)
    {
        // Create renderable objects for each
        // defined token
        var items = renderers.SelectMany(r => r.Render(logEvent)).ToArray();

        // Join all renderable objects
        var collection = new RenderableCollection(items);

        // Write them to the console
        AnsiConsole.Write(collection);
    }

    private static IEnumerable<ITemplateTokenRenderer> InitializeRenders(string outputTemplate)
    {
        var template = new MessageTemplateParser().Parse(outputTemplate);
        foreach (var token in template.Tokens)
        {
            if (!TryInitializeRender(token, out var renderer)) continue;
            if (renderer is null) continue;
            yield return renderer;
        }
    }

    private static bool TryInitializeRender(MessageTemplateToken token, out ITemplateTokenRenderer? renderer)
    {
        switch (token)
        {
            case TextToken tt:
                renderer = new TextTokenRenderer(tt.Text);
                return true;
            case PropertyToken pt:
                return TryInitializePropertyRender(pt, out renderer);
            default:
                renderer = null;
                return false;
        }
    }

    private static bool TryInitializePropertyRender(PropertyToken propertyToken, out ITemplateTokenRenderer renderer)
    {
        renderer = GetPropertyRender(propertyToken);
        return true;
    }

    private static ITemplateTokenRenderer GetPropertyRender(PropertyToken propertyToken)
    {
        return propertyToken.PropertyName switch
        {
            OutputProperties.LevelPropertyName      => new LevelTokenRenderer(propertyToken),
            OutputProperties.NewLinePropertyName    => new NewLineTokenRenderer(),
            OutputProperties.ExceptionPropertyName  => new ExceptionTokenRenderer(),
            OutputProperties.MessagePropertyName    => new MessageTemplateOutputTokenRenderer(),
            OutputProperties.TimestampPropertyName  => new TimestampTokenRenderer(propertyToken),
            OutputProperties.PropertiesPropertyName => new PropertyTemplateRenderer(propertyToken),
            _                                       => new EventPropertyTokenRenderer(propertyToken)
        };
    }
}
