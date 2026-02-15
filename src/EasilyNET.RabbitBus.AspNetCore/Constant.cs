namespace EasilyNET.RabbitBus.AspNetCore;

internal static class Constant
{
    internal const string OptionName = "easily_net_rabbit_bus";

    internal const string ActivitySourceName = "EasilyNET.RabbitBus";

    internal const string PublishPipelineName = "easilynet-rabbitbus-publish-resilience-pipeline";

    internal const string ConnectionPipelineName = "easilynet-rabbitbus-connection-resilience-pipeline";

    internal const string HandlerPipelineName = "easilynet-rabbitbus-handler-resilience-pipeline";
}