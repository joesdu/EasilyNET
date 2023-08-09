// See https://aka.ms/new-console-template for more information

using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder().ConfigureServices(ConfigureServices).UsePropertyInjection();
await builder.RunConsoleAsync();

public partial class Program
{
    private static void ConfigureServices(IServiceCollection serviceCollection) =>
        serviceCollection
            .AddHostedService<HelloWorldSetter>()
            .AddTransient<IConsole, CustomConsole>();
}

public interface IHelloWorld
{
    void Hello();
}

public abstract class HelloWorld : IHelloWorld, IHostedService
{
    [Injection]
    private IHostApplicationLifetime _appLifetime;

    public abstract void Hello();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Hello();
        _appLifetime.StopApplication();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class CustomConsole : IConsole
{
    /// <inheritdoc />
    public void WriteLine(string s)
    {
        Console.WriteLine(s);
    }
}

public interface IConsole
{
    void WriteLine(string s);
}

public class HelloWorldSetter : HelloWorld
{
    [Injection]
    public IConsole Console { get; set; }

    /// <inheritdoc />
    public override void Hello()
    {
        Console.WriteLine("HelloWorld");
    }
}