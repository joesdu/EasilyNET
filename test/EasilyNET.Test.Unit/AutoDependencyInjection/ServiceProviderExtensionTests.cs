using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.AutoDependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.Test.Unit.AutoDependencyInjection;

[TestClass]
public sealed class ServiceProviderExtensionTests
{
    [TestInitialize]
    public void Setup() => ClearRegistries();

    [TestCleanup]
    public void Cleanup() => ClearRegistries();

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
        try
        {
            var services = new ServiceCollection();
            services.AddTransient<IWelcomeService, WelcomeService>();
            using var provider = services.BuildServiceProvider();
            RegisterImplementation(typeof(IWelcomeService), typeof(WelcomeService));
            // Cast to the base type to satisfy nullable analysis for the params array.
            // ReSharper disable once RedundantExplicitParamsArrayCreation
            var welcome = provider.Resolve<IWelcomeService>([new NamedParameter("name", "Rose")]);
            Assert.AreEqual("Hello, Rose", welcome.Greet());
        }
        finally
        {
            ClearRegistries();
        }
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

    private static void ClearRegistries()
    {
        GetServiceImplementations().Clear();
        ClearNamedServices();
    }

    private static void RegisterImplementation(Type serviceType, Type implementationType)
    {
        var serviceImplementations = GetServiceImplementations();
        serviceImplementations[serviceType] = implementationType;
    }

    private static ConcurrentDictionary<Type, Type> GetServiceImplementations()
    {
        var field = typeof(ServiceProviderExtension).GetField("ServiceImplementations", BindingFlags.NonPublic | BindingFlags.Static);
        return field?.GetValue(null) as ConcurrentDictionary<Type, Type> ?? throw new InvalidOperationException("Unable to read ServiceImplementations dictionary via reflection.");
    }

    private static void ClearNamedServices()
    {
        var field = typeof(ServiceProviderExtension).GetField("NamedServices", BindingFlags.NonPublic | BindingFlags.Static);
        var dictionary = field?.GetValue(null);
        var clearMethod = dictionary?.GetType().GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
        clearMethod?.Invoke(dictionary, null);
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