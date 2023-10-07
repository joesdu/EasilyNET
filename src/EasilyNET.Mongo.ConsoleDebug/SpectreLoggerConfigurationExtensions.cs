using EasilyNET.Mongo.ConsoleDebug;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Serilog;

/// <summary>
/// 扩展类
/// </summary>
public static class SpectreLoggerConfigurationExtensions
{
    private const string DefaultConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Write log events to the console using Spectre.Console.
    /// </summary>
    /// <param name="loggerConfiguration">Logger sink configuration.</param>
    /// <param name="outputTemplate">
    /// A message template describing the format used to write to the sink.
    /// The default is "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}".
    /// </param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for
    /// events passed through the sink. Ignored when <paramref name="levelSwitch" /> is specified.
    /// </param>
    /// <param name="levelSwitch">
    /// A switch allowing the pass-through minimum level
    /// to be changed at runtime.
    /// </param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration SpectreConsole(this LoggerSinkConfiguration loggerConfiguration,
        string outputTemplate = DefaultConsoleOutputTemplate,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null) =>
        loggerConfiguration.Sink(new SpectreConsoleSink(outputTemplate), restrictedToMinimumLevel, levelSwitch);
}