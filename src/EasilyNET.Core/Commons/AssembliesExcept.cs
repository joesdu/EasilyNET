namespace EasilyNET.Core.Commons;

/// <summary>
/// 用于排除的程序集
/// </summary>
internal static class AssembliesExcept
{
    /// <summary>
    /// 内置程序集排除项,便于忽略掉一些非必要的程序集反射,加快性能
    /// </summary>
    internal static readonly HashSet<string> Except =
    [
        "Accessibility",
        "Acornima",
        "aspnetcore",
        "Autofac",
        "BACnet",
        "BouncyCastle",
        "clr",
        "CommunityToolkit",
        "coreclr",
        "D3DCompiler",
        "DirectWriteForwarder",
        "DnsClient",
        "Google",
        "Grpc",
        "hostfxr",
        "hostpolicy",
        "Jint",
        "Json",
        "Knx",
        "lib60870",
        "LiteDB",
        "MessagePack",
        "Microsoft",
        "MongoDB",
        "MQTTnet",
        "mscordaccore",
        "mscordbi",
        "mscorlib",
        "mscorrc",
        "msquic",
        "netstandard",
        "Newtonsoft",
        "Opc",
        "OpenTelemetry",
        "PacketDotNet",
        "PenImc_cor3",
        "Pipelines",
        "Polly",
        "PortableDI",
        "Presentation",
        "RabbitMQ",
        "ReachFramework",
        "Serilog",
        "Sharp",
        "Snappier",
        "Spectre",
        "StackExchange",
        "Standard",
        "Swashbuckle",
        "System",
        "UIAutomation",
        "vcruntime",
        "Windows",
        "wpfgfx",
        "ZstdSharp"
    ];
}