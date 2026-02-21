using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.Test.Unit.AutoDependencyInjection;

[TestClass]
public sealed class ServiceProviderExtensionTests
{
    [TestMethod]
    public void Resolve_ShouldReturnRegisteredService()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreetingService, GreetingService>();
        using var provider = services.BuildServiceProvider();
        var greeting = provider.Resolve<IGreetingService>();
        Assert.AreEqual("Hello", greeting.SayHello());
    }

    [TestMethod]
    public void ResolveKeyed_ShouldLeverageBuiltInKeyedRegistrations()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreetingService>("evening", (_, _) => new KeyedGreetingService("Evening"));
        using var provider = services.BuildServiceProvider();
        var greeting = provider.ResolveKeyed<IGreetingService>("evening");
        Assert.AreEqual("Evening", greeting.SayHello());
    }

    [TestMethod]
    public void CreateResolver_ShouldReturnValidResolver()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreetingService, GreetingService>();
        using var provider = services.BuildServiceProvider();
        using var resolver = provider.CreateResolver();
        var greeting = resolver.Resolve<IGreetingService>();
        Assert.AreEqual("Hello", greeting.SayHello());
    }

    [TestMethod]
    public void ResolveNamed_ShouldWorkWithKeyedServices()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreetingService>("morning", (_, _) => new KeyedGreetingService("Good Morning"));
        using var provider = services.BuildServiceProvider();
        var greeting = provider.ResolveNamed<IGreetingService>("morning");
        Assert.AreEqual("Good Morning", greeting.SayHello());
    }

    private interface IGreetingService
    {
        string SayHello();
    }

    private sealed class GreetingService : IGreetingService
    {
        public string SayHello() => "Hello";
    }

    private sealed class KeyedGreetingService(string message) : IGreetingService
    {
        public string SayHello() => message;
    }
}