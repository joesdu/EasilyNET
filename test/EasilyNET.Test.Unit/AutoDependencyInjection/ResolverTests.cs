using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.Test.Unit.AutoDependencyInjection;

/// <summary>
/// Comprehensive tests for Resolver, Parameter types, Owned, IIndex, and parameterized factories.
/// </summary>
[TestClass]
public sealed class ResolverTests
{
    #region Resolver core — parameter overrides

    [TestMethod]
    public void Resolve_WithNamedParameter_ShouldOverrideConstructorArg()
    {
        // Use concrete type (non-interface) so Resolver can find the implementation without ServiceRegistry
        using var provider = BuildProvider(sc => sc.AddTransient<Greeter>());
        using var resolver = provider.CreateResolver();
        var greeter = resolver.Resolve<Greeter>(new NamedParameter("name", "Alice"));
        Assert.AreEqual("Hello, Alice", greeter.Greet());
    }

    [TestMethod]
    public void Resolve_WithTypedParameter_ShouldOverrideConstructorArg()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<Greeter>());
        using var resolver = provider.CreateResolver();
        var greeter = resolver.Resolve<Greeter>(new TypedParameter(typeof(string), "Bob"));
        Assert.AreEqual("Hello, Bob", greeter.Greet());
    }

    [TestMethod]
    public void Resolve_WithPositionalParameter_ShouldOverrideByPosition()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<MultiArgService>());
        using var resolver = provider.CreateResolver();
        var svc = resolver.Resolve<MultiArgService>(new PositionalParameter(0, "first"),
            new PositionalParameter(1, 42));
        Assert.AreEqual("first:42", svc.GetValue());
    }

    [TestMethod]
    public void Resolve_WithResolvedParameter_ShouldUsePredicate()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<Greeter>());
        using var resolver = provider.CreateResolver();
        var greeter = resolver.Resolve<Greeter>(new ResolvedParameter((type, name) => type == typeof(string) && name == "name",
            (_, _, _) => "Charlie"));
        Assert.AreEqual("Hello, Charlie", greeter.Greet());
    }

    [TestMethod]
    public void Resolve_WithMixedParameters_ShouldApplyAll()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<MultiArgService>());
        using var resolver = provider.CreateResolver();
        var svc = resolver.Resolve<MultiArgService>(new NamedParameter("label", "test"),
            new TypedParameter(typeof(int), 99));
        Assert.AreEqual("test:99", svc.GetValue());
    }

    [TestMethod]
    public void Resolve_WithoutParameters_ShouldResolveFromContainer()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<ISimpleService, SimpleService>());
        using var resolver = provider.CreateResolver();
        var svc = resolver.Resolve<ISimpleService>();
        Assert.IsNotNull(svc);
        Assert.AreEqual("simple", svc.Name());
    }

    [TestMethod]
    public void Resolve_MissingService_ShouldThrow()
    {
        using var provider = BuildProvider(_ => { });
        using var resolver = provider.CreateResolver();
        Assert.ThrowsExactly<InvalidOperationException>(() => resolver.Resolve<ISimpleService>());
    }

    [TestMethod]
    public void Resolve_NoSuitableConstructor_ShouldThrowWithDetails()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<Greeter>());
        using var resolver = provider.CreateResolver();
        // Greeter needs a string, but we provide an int — no constructor can be satisfied
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            resolver.Resolve<Greeter>(new TypedParameter(typeof(int), 123)));
        Assert.Contains("No suitable constructor found", ex.Message);
        Assert.Contains("Greeter", ex.Message);
    }

    #endregion

    #region Keyed / Named resolution

    [TestMethod]
    public void ResolveKeyed_ShouldResolveByKey()
    {
        using var provider = BuildProvider(sc =>
            sc.AddKeyedTransient<ISimpleService>("fast", (_, _) => new NamedSimpleService("fast")));
        using var resolver = provider.CreateResolver();
        var svc = resolver.ResolveKeyed<ISimpleService>("fast");
        Assert.AreEqual("fast", svc.Name());
    }

    [TestMethod]
    public void ResolveNamed_ShouldDelegateToResolveKeyed()
    {
        using var provider = BuildProvider(sc =>
            sc.AddKeyedTransient<ISimpleService>("slow", (_, _) => new NamedSimpleService("slow")));
        using var resolver = provider.CreateResolver();
        var svc = resolver.ResolveNamed<ISimpleService>("slow");
        Assert.AreEqual("slow", svc.Name());
    }

    [TestMethod]
    public void ResolveKeyed_MissingKey_ShouldThrow()
    {
        using var provider = BuildProvider(_ => { });
        using var resolver = provider.CreateResolver();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            resolver.ResolveKeyed<ISimpleService>("nonexistent"));
    }

    #endregion

    #region Owned<T>

    [TestMethod]
    public void ResolveOwned_ShouldReturnServiceInIsolatedScope()
    {
        using var provider = BuildProvider(sc => sc.AddScoped<ISimpleService, SimpleService>());
        var owned = provider.ResolveOwned<ISimpleService>();
        Assert.IsNotNull(owned);
        Assert.IsNotNull(owned.Value);
        Assert.AreEqual("simple", owned.Value.Name());
        owned.Dispose();
    }

    [TestMethod]
    public void ResolveOwned_DisposeShouldDisposeScope()
    {
        using var provider = BuildProvider(sc => sc.AddScoped<DisposableService>());
        var owned = provider.ResolveOwned<DisposableService>();
        var svc = owned.Value;
        Assert.IsFalse(svc.IsDisposed);
        owned.Dispose();
        Assert.IsTrue(svc.IsDisposed);
    }

    [TestMethod]
    public void ResolveOwned_DoubleDisposeShouldNotThrow()
    {
        using var provider = BuildProvider(sc => sc.AddScoped<ISimpleService, SimpleService>());
        var owned = provider.ResolveOwned<ISimpleService>();
        owned.Dispose();
        owned.Dispose(); // should not throw
    }

    [TestMethod]
    public void ResolveOwned_ScopedServicesShouldBeIsolated()
    {
        using var provider = BuildProvider(sc => sc.AddScoped<DisposableService>());
        var owned1 = provider.ResolveOwned<DisposableService>();
        var owned2 = provider.ResolveOwned<DisposableService>();
        Assert.AreNotSame(owned1.Value, owned2.Value);
        owned1.Dispose();
        Assert.IsTrue(owned1.Value.IsDisposed);
        Assert.IsFalse(owned2.Value.IsDisposed); // independent scope
        owned2.Dispose();
    }

    #endregion

    #region IIndex<TKey, TService>

    [TestMethod]
    public void IIndex_ShouldResolveKeyedServices()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddKeyedTransient<ISimpleService>("a", (_, _) => new NamedSimpleService("alpha"));
            sc.AddKeyedTransient<ISimpleService>("b", (_, _) => new NamedSimpleService("beta"));
            sc.AddTransient<IIndex<string, ISimpleService>>(sp =>
                new KeyedServiceIndexAdapter<string, ISimpleService>(sp));
        });
        var index = provider.GetRequiredService<IIndex<string, ISimpleService>>();
        Assert.AreEqual("alpha", index["a"].Name());
        Assert.AreEqual("beta", index["b"].Name());
    }

    [TestMethod]
    public void IIndex_TryGet_ShouldReturnFalseForMissingKey()
    {
        using var provider = BuildProvider(sc =>
            sc.AddTransient<IIndex<string, ISimpleService>>(sp =>
                new KeyedServiceIndexAdapter<string, ISimpleService>(sp)));
        var index = provider.GetRequiredService<IIndex<string, ISimpleService>>();
        var found = index.TryGet("missing", out var svc);
        Assert.IsFalse(found);
        Assert.IsNull(svc);
    }

    [TestMethod]
    public void IIndex_TryGet_ShouldReturnTrueForExistingKey()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddKeyedTransient<ISimpleService>("x", (_, _) => new NamedSimpleService("xray"));
            sc.AddTransient<IIndex<string, ISimpleService>>(sp =>
                new KeyedServiceIndexAdapter<string, ISimpleService>(sp));
        });
        var index = provider.GetRequiredService<IIndex<string, ISimpleService>>();
        var found = index.TryGet("x", out var svc);
        Assert.IsTrue(found);
        Assert.IsNotNull(svc);
        Assert.AreEqual("xray", svc.Name());
    }

    #endregion

    #region Parameterized Factory (Func<X, T>)

    [TestMethod]
    public void ParameterizedFactory_Func1_ShouldResolveWithParam()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<Greeter>();
            sc.AddParameterizedFactory<string, Greeter>();
        });
        var factory = provider.GetRequiredService<Func<string, Greeter>>();
        var greeter = factory("Dave");
        Assert.AreEqual("Hello, Dave", greeter.Greet());
    }

    [TestMethod]
    public void ParameterizedFactory_Func2_ShouldResolveWithTwoParams()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<MultiArgService>();
            sc.AddParameterizedFactory<string, int, MultiArgService>();
        });
        var factory = provider.GetRequiredService<Func<string, int, MultiArgService>>();
        var svc = factory("test", 7);
        Assert.AreEqual("test:7", svc.GetValue());
    }

    [TestMethod]
    public void OwnedFactory_ShouldCreateLifetimeControlledInstances()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddScoped<DisposableService>();
            sc.AddOwnedFactory<DisposableService>();
        });
        var factory = provider.GetRequiredService<Func<Owned<DisposableService>>>();
        var owned = factory();
        Assert.IsFalse(owned.Value.IsDisposed);
        owned.Dispose();
        Assert.IsTrue(owned.Value.IsDisposed);
    }

    [TestMethod]
    public void OwnedFactory_EachCallCreatesNewScope()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddScoped<DisposableService>();
            sc.AddOwnedFactory<DisposableService>();
        });
        var factory = provider.GetRequiredService<Func<Owned<DisposableService>>>();
        var owned1 = factory();
        var owned2 = factory();
        Assert.AreNotSame(owned1.Value, owned2.Value);
        owned1.Dispose();
        Assert.IsTrue(owned1.Value.IsDisposed);
        Assert.IsFalse(owned2.Value.IsDisposed);
        owned2.Dispose();
    }

    #endregion

    #region Parameterized Factory — duplicate type regression

    [TestMethod]
    public void ParameterizedFactory_Func2_DuplicateTypes_ShouldPreserveArgumentOrder()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<DuplicateTypeService>();
            sc.AddParameterizedFactory<string, string, DuplicateTypeService>();
        });
        var factory = provider.GetRequiredService<Func<string, string, DuplicateTypeService>>();
        var svc = factory("hello", "world");
        Assert.AreEqual("hello|world", svc.GetValue());
    }

    [TestMethod]
    public void ParameterizedFactory_Func3_DuplicateTypes_ShouldPreserveArgumentOrder()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<TripleDuplicateTypeService>();
            sc.AddParameterizedFactory<string, string, string, TripleDuplicateTypeService>();
        });
        var factory = provider.GetRequiredService<Func<string, string, string, TripleDuplicateTypeService>>();
        var svc = factory("x", "y", "z");
        Assert.AreEqual("x|y|z", svc.GetValue());
    }

    #endregion

    #region Parameterized Factory — 4-param and general-purpose object[] factory

    [TestMethod]
    public void ParameterizedFactory_Func4_ShouldResolveWithFourParams()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<FourArgService>();
            sc.AddParameterizedFactory<string, int, bool, string, FourArgService>();
        });
        var factory = provider.GetRequiredService<Func<string, int, bool, string, FourArgService>>();
        var svc = factory("hello", 42, true, "world");
        Assert.AreEqual("hello|42|True|world", svc.GetValue());
    }

    [TestMethod]
    public void ParameterizedFactory_ObjectArray_ShouldResolveWithManyParams()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<SixArgService>();
            sc.AddParameterizedFactory<SixArgService>(typeof(string), typeof(int), typeof(bool), typeof(string), typeof(double), typeof(string));
        });
        var factory = provider.GetRequiredService<Func<object[], SixArgService>>();
        var svc = factory(["a", 1, false, "b", 2.5, "c"]);
        Assert.AreEqual("a|1|False|b|2.5|c", svc.GetValue());
    }

    [TestMethod]
    public void ParameterizedFactory_ObjectArray_WrongArgCount_ShouldThrow()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<SixArgService>();
            sc.AddParameterizedFactory<SixArgService>(typeof(string), typeof(int), typeof(bool), typeof(string), typeof(double), typeof(string));
        });
        var factory = provider.GetRequiredService<Func<object[], SixArgService>>();
        Assert.ThrowsExactly<ArgumentException>(() => factory(["only", "two"]));
    }

    [TestMethod]
    public void ParameterizedFactory_ObjectArray_WrongArgType_ShouldThrow()
    {
        using var provider = BuildProvider(sc =>
        {
            sc.AddTransient<SixArgService>();
            sc.AddParameterizedFactory<SixArgService>(typeof(string), typeof(int), typeof(bool), typeof(string), typeof(double), typeof(string));
        });
        var factory = provider.GetRequiredService<Func<object[], SixArgService>>();
        // Position 1 expects int, but we pass a string
        Assert.ThrowsExactly<ArgumentException>(() => factory(["a", "not-an-int", false, "b", 2.5, "c"]));
    }

    #endregion

    #region CreateResolver extension

    [TestMethod]
    public void CreateResolver_WithScope_ShouldCreateIsolatedScope()
    {
        using var provider = BuildProvider(sc => sc.AddScoped<ISimpleService, SimpleService>());
        using var resolver = provider.CreateResolver(true);
        var svc = resolver.Resolve<ISimpleService>();
        Assert.IsNotNull(svc);
    }

    [TestMethod]
    public void CreateResolver_WithoutScope_ShouldUseParentProvider()
    {
        using var provider = BuildProvider(sc => sc.AddTransient<ISimpleService, SimpleService>());
        using var resolver = provider.CreateResolver();
        var svc = resolver.Resolve<ISimpleService>();
        Assert.IsNotNull(svc);
    }

    #endregion

    #region Helpers

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Test adapter for IIndex that wraps IServiceProvider keyed service resolution.
    /// Used in tests since the internal KeyedServiceIndex is not accessible.
    /// </summary>
    private sealed class KeyedServiceIndexAdapter<TKey, TService>(IServiceProvider provider) : IIndex<TKey, TService>
        where TKey : notnull
        where TService : notnull
    {
        public TService this[TKey key] => provider.GetRequiredKeyedService<TService>(key);

        public bool TryGet(TKey key, out TService? service)
        {
            service = provider.GetKeyedService<TService>(key);
            return service is not null;
        }
    }

    // Test types — public so Resolver reflection can access constructors

    public interface ISimpleService
    {
        string Name();
    }

    public sealed class SimpleService : ISimpleService
    {
        public string Name() => "simple";
    }

    public sealed class NamedSimpleService(string name) : ISimpleService
    {
        public string Name() => name;
    }

    public interface IGreeter
    {
        string Greet();
    }

    public sealed class Greeter(string name) : IGreeter
    {
        public string Greet() => $"Hello, {name}";
    }

    public interface IMultiArgService
    {
        string GetValue();
    }

    public sealed class MultiArgService(string label, int count) : IMultiArgService
    {
        public string GetValue() => $"{label}:{count}";
    }

    public sealed class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// Service with two constructor parameters of the same type (string, string).
    /// Used to verify positional parameter matching when types are duplicated.
    /// </summary>
    public sealed class DuplicateTypeService(string first, string second)
    {
        public string GetValue() => $"{first}|{second}";
    }

    /// <summary>
    /// Service with three constructor parameters of the same type (string, string, string).
    /// Used to verify positional parameter matching when types are duplicated.
    /// </summary>
    public sealed class TripleDuplicateTypeService(string a, string b, string c)
    {
        public string GetValue() => $"{a}|{b}|{c}";
    }

    /// <summary>
    /// Service with four constructor parameters for testing the 4-param factory overload.
    /// </summary>
    public sealed class FourArgService(string a, int b, bool c, string d)
    {
        public string GetValue() => $"{a}|{b}|{c}|{d}";
    }

    /// <summary>
    /// Service with six constructor parameters for testing the general-purpose object[] factory.
    /// </summary>
    public sealed class SixArgService(string a, int b, bool c, string d, double e, string f)
    {
        public string GetValue() => $"{a}|{b}|{c}|{d}|{e}|{f}";
    }

    #endregion
}