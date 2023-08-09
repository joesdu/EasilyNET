using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Quartz;
using WebApi.Test.Unit.Services.Abstraction;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.QzJobs;

/// <summary>
/// 测试属性注入使用Qz
/// </summary>
[DisallowConcurrentExecution]
public class PropertyInjectionTestJob : IJob
{
    [Injection]
    private readonly ILogger<PropertyInjectionTestJob>? _logger = null;

    /// <summary>
    /// PropertyInjectionTestService
    /// </summary>
    [Injection]
    private readonly IPropertyInjectionTestService? _propertyInjectionTestService = null;

    /// <inheritdoc />
    public Task Execute(IJobExecutionContext context)
    {
        _logger?.LogInformation("从{Class}类中使用{Logger}输出日志信息", nameof(PropertyInjectionTestJob), nameof(_logger));
        _propertyInjectionTestService?.Execute();
        if (_logger is null)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"{nameof(PropertyInjectionTestJob)}类中的logger为null");
        }
        if (_propertyInjectionTestService is not null) return Task.CompletedTask;
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"{nameof(PropertyInjectionTestJob)}类中的PropertyInjectionTestService为null");
        return Task.CompletedTask;
    }
}