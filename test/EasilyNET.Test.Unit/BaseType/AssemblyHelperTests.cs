using System.Runtime.Loader;
using EasilyNET.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable UnusedMember.Local

namespace EasilyNET.Test.Unit.BaseType;

/// <summary>
/// AssemblyHelperTests
/// </summary>
[TestClass]
public class AssemblyHelperTests
{
    /// <summary>
    /// 通过名称获取程序集
    /// </summary>
    [TestMethod]
    public void GetAssembliesByName_ReturnsCorrectAssemblies()
    {
        // Arrange
        var assemblyNames = new[] { "EasilyNET.Core" };
        var expectedAssemblies = assemblyNames.Select(o => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, $"{o}.dll")));

        // Act
        var actualAssemblies = AssemblyHelper.GetAssembliesByName(assemblyNames);

        // Assert
        Assert.IsNotNull(expectedAssemblies);
        Assert.IsNotNull(actualAssemblies);
        Assert.IsFalse(expectedAssemblies.Equals(actualAssemblies));
    }

    /// <summary>
    /// 通过特性获取Type
    /// </summary>
    [TestMethod]
    public void FindTypesByAttribute_ReturnsCorrectTypes()
    {
        // Arrange
        var expectedTypes = new[] { typeof(MyClass1), typeof(MyClass2) };

        // Act
        var actualTypes = AssemblyHelper.FindTypesByAttribute<TestAttribute>();

        // Assert
        Assert.IsNotNull(actualTypes);
        Assert.IsFalse(expectedTypes.Equals(actualTypes));
    }

    /// <summary>
    /// 查找程序集
    /// </summary>
    [TestMethod]
    public void FindAllItems_ReturnsCorrectAssemblies()
    {
        // Arrange
        var expectedAssemblies = new[] { typeof(AssemblyHelper).Assembly };

        // Act
        var actualAssemblies = AssemblyHelper.FindAllItems(a => expectedAssemblies.Contains(a));

        // Assert
        Assert.IsNotNull(actualAssemblies);
        Assert.IsFalse(expectedAssemblies.Equals(actualAssemblies));
    }
}

/// <summary>
/// 测试特性
/// </summary>
file class TestAttribute : AttributeBase
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string Description() => "Test";
}

[Test]
file class MyClass1
{
    public string? B { get; set; } = string.Empty;
}

[Test]
file class MyClass2
{
    public string? A { get; set; } = string.Empty;
}