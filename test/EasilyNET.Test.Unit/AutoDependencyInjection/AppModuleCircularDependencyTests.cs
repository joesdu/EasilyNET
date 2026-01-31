using EasilyNET.AutoDependencyInjection.Attributes;
using EasilyNET.AutoDependencyInjection.Modules;

namespace EasilyNET.Test.Unit.AutoDependencyInjection;

/// <summary>
/// Tests for circular dependency detection in AppModule
/// </summary>
[TestClass]
public sealed class AppModuleCircularDependencyTests
{
    [TestMethod]
    public void GetDependedTypes_ShouldThrowWithChainMessage_WhenCircularDependencyExists()
    {
        // Arrange
        var moduleA = new ModuleA();

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = moduleA.GetDependedTypes().ToList());

        // Verify the error message contains the complete dependency chain
        Assert.Contains("Circular dependency detected:", exception.Message);
        Assert.Contains("ModuleA -> ModuleB -> ModuleC -> ModuleA", exception.Message);
        Assert.Contains("Module dependencies must form a directed acyclic graph (DAG)", exception.Message);
    }

    [TestMethod]
    public void GetDependedTypes_ShouldThrowWithChainMessage_WhenSelfCircularDependencyExists()
    {
        // Arrange
        var moduleD = new ModuleD();

        // Act & Assert
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => _ = moduleD.GetDependedTypes().ToList());

        // Verify the error message contains the complete dependency chain
        Assert.Contains("Circular dependency detected:", exception.Message);
        Assert.Contains("ModuleD -> ModuleE -> ModuleD", exception.Message);
    }

    [TestMethod]
    public void GetDependedTypes_ShouldNotThrow_WhenNoDependencies()
    {
        // Arrange
        var moduleF = new ModuleF();

        // Act
        var dependencies = moduleF.GetDependedTypes();

        // Assert
        Assert.IsNotNull(dependencies);
        Assert.IsFalse(dependencies.Any());
    }

    [TestMethod]
    public void GetDependedTypes_ShouldNotThrow_WhenValidDependencyChain()
    {
        // Arrange
        var moduleG = new ModuleG();

        // Act
        var dependencies = moduleG.GetDependedTypes().ToList();

        // Assert
        Assert.IsNotNull(dependencies);
        // ModuleG depends on ModuleF (which has no dependencies)
        // So we should get ModuleF in the result
        Assert.HasCount(1, dependencies);
        Assert.AreEqual(typeof(ModuleF), dependencies[0]);
    }

    // Test modules for circular dependency: A -> B -> C -> A
    [DependsOn(typeof(ModuleB))]
    private sealed class ModuleA : AppModule;

    [DependsOn(typeof(ModuleC))]
    private sealed class ModuleB : AppModule;

    [DependsOn(typeof(ModuleA))]
    private sealed class ModuleC : AppModule;

    // Test modules for smaller circular dependency: D -> E -> D
    [DependsOn(typeof(ModuleE))]
    private sealed class ModuleD : AppModule;

    [DependsOn(typeof(ModuleD))]
    private sealed class ModuleE : AppModule;

    // Test module with no dependencies
    private sealed class ModuleF : AppModule;

    // Test module with valid dependency chain
    [DependsOn(typeof(ModuleF))]
    private sealed class ModuleG : AppModule;
}
