using WebApi.Test.Unit.Controllers;

namespace WebApi.Test.Unit.Decorators;

/// <inheritdoc />
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class TestNoDefaultDecorator(FooService jc, string str, ILogger<TestNoDefaultDecorator> logger) : IFooService
{
    /// <inheritdoc />
    public string SayHello()
    {
        logger.LogInformation("Before DoWork");
        var hello = jc.GetHello(str);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("DoWork Result: {Hello}", hello);
        }
        logger.LogInformation("After DoWork");
        return hello;
    }
}