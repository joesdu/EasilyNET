using EasilyNET.AutoDependencyInjection.Core.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using WebApi.Test.Unit.Services.Abstraction;

// ReSharper disable UnusedType.Global

namespace WebApi.Test.Unit.Services;

/// <inheritdoc cref="IPropertyInjectionTestService" />
public class PropertyInjectionTestService : IPropertyInjectionTestService, ISingletonDependency
{
    [Injection]
    private readonly ILogger<PropertyInjectionTestService>? logger = null;

    /// <inheritdoc />
    public Task Execute()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"从{nameof(PropertyInjectionTestService)}类中输出信息");
        logger?.LogInformation("使用{Logger}从{Class}中输出信息", nameof(logger), nameof(PropertyInjectionTestService));
        if (logger is not null) return Task.CompletedTask;
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"{nameof(PropertyInjectionTestService)}类中的logger为null");
        return Task.CompletedTask;
    }
}