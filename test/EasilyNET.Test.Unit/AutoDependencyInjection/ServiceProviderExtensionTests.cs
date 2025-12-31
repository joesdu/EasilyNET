using EasilyNET.AutoDependencyInjection;
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
    public void Resolve_WithParameters_ShouldOverrideConstructorArguments()
    {
        var services = new ServiceCollection();
        // 注册 ServiceRegistry 以支持参数覆盖
        var registry = new ServiceRegistry();
        registry.RegisterImplementation(typeof(IWelcomeService), typeof(WelcomeService));
        services.AddSingleton(registry);
        services.AddTransient<IWelcomeService, WelcomeService>();
        using var provider = services.BuildServiceProvider();
        // 使用 NamedParameter 覆盖构造函数参数
        var welcome = provider.Resolve<IWelcomeService>(new NamedParameter("name", "Rose"));
        Assert.AreEqual("Hello, Rose", welcome.Greet());
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
    public void TryResolve_ShouldReturnFalseForMissingService()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var resolved = provider.TryResolve(typeof(IGreetingService), out var instance);
        Assert.IsFalse(resolved);
        Assert.IsNull(instance);
    }

    [TestMethod]
    public void TryResolve_Generic_ShouldReturnTrueForRegisteredService()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreetingService, GreetingService>();
        using var provider = services.BuildServiceProvider();
        var resolved = provider.TryResolve<IGreetingService>(out var instance);
        Assert.IsTrue(resolved);
        Assert.IsNotNull(instance);
        Assert.AreEqual("Hello", instance.SayHello());
    }

    [TestMethod]
    public void ResolveOptional_ShouldReturnNullForMissingService()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var instance = provider.ResolveOptional<IGreetingService>();
        Assert.IsNull(instance);
    }

    [TestMethod]
    public void ResolveOptional_ShouldReturnInstanceForRegisteredService()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreetingService, GreetingService>();
        using var provider = services.BuildServiceProvider();
        var instance = provider.ResolveOptional<IGreetingService>();
        Assert.IsNotNull(instance);
        Assert.AreEqual("Hello", instance.SayHello());
    }

    [TestMethod]
    public void ResolveAll_ShouldReturnAllRegistrations()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreetingService, GreetingService>();
        services.AddTransient<IGreetingService>(_ => new KeyedGreetingService("Hi"));
        using var provider = services.BuildServiceProvider();
        var all = provider.ResolveAll<IGreetingService>().ToList();
        Assert.HasCount(2, all);
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
    public void BeginResolverScope_ShouldCreateScopedResolver()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGreetingService, GreetingService>();
        using var provider = services.BuildServiceProvider();
        using var scopedResolver = provider.BeginResolverScope();
        var greeting = scopedResolver.Resolve<IGreetingService>();
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

    private interface IWelcomeService
    {
        string Greet();
    }

    private sealed class WelcomeService(string name) : IWelcomeService
    {
        public string Greet() => $"Hello, {name}";
    }
}