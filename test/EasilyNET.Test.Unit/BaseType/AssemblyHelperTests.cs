using System.Reflection;
using System.Runtime.Loader;
using EasilyNET.Core.Attributes;
using EasilyNET.Core.Misc;

// ReSharper disable UnusedMember.Local

namespace EasilyNET.Test.Unit.BaseType;

/// <summary>
/// AssemblyHelperTests
/// </summary>
[TestClass]
public class AssemblyHelperTests
{
    [TestInitialize]
    public void Init()
    {
        AssemblyHelper.Configure(o =>
        {
            o.ScanAllRuntimeLibraries = false;
            o.AllowDirectoryProbe = false;
        });
        AssemblyHelper.AddIncludePatterns("EasilyNET.*");
        AssemblyHelper.ClearCaches();
    }

    /// <summary>
    /// 通过名称获取程序集
    /// </summary>
    [TestMethod]
    public void GetAssembliesByName_ReturnsCorrectAssemblies()
    {
        // Arrange
        var assemblyNames = new[] { "EasilyNET.Core" };
        var expectedPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyNames[0]}.dll");
        _ = AssemblyLoadContext.Default.LoadFromAssemblyPath(expectedPath);

        // Act
        var actualAssemblies = AssemblyHelper.GetAssembliesByName(assemblyNames).Where(a => a is not null).Cast<Assembly>().ToArray();

        // Assert
        Assert.IsTrue(actualAssemblies.Any(a => string.Equals(a.GetName().Name, "EasilyNET.Core", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// 通过特性获取Type
    /// </summary>
    [TestMethod]
    public void FindTypesByAttribute_ReturnsCorrectTypes()
    {
        // Act
        var actualTypes = AssemblyHelper.FindTypesByAttribute<TestAttribute>().ToArray();

        // Assert
        Assert.IsTrue(actualTypes.Contains(typeof(MyClass1)));
        Assert.IsTrue(actualTypes.Contains(typeof(MyClass2)));
    }

    /// <summary>
    /// 查找程序集
    /// </summary>
    [TestMethod]
    public void FindAllItems_ReturnsCorrectAssemblies()
    {
        // Arrange
        var target = typeof(AssemblyHelper).Assembly;

        // Act
        var actualAssemblies = AssemblyHelper.FindAllItems(a => a == target).ToArray();

        // Assert
        Assert.IsGreaterThanOrEqualTo(actualAssemblies.Length, 1);
        Assert.IsTrue(actualAssemblies.Contains(target));
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