using EasilyNET.Core.Attributes;
using EasilyNET.Core.BaseType;
using System.Runtime.Loader;

namespace EasilyNET.Core.Test.Unit.BaseType;

/// <summary>
/// AssemblyHelperTests
/// </summary>
public class AssemblyHelperTests
{
    /// <summary>
    /// 通过名称获取程序集
    /// </summary>
    [Fact]
    public void GetAssembliesByName_ReturnsCorrectAssemblies()
    {
        // Arrange
        var assemblyNames = new[] { "EasilyNET.Core" };
        var expectedAssemblies = assemblyNames.Select(o => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, $"{o}.dll")));

        // Act
        var actualAssemblies = AssemblyHelper.GetAssembliesByName(assemblyNames);

        // Assert
        Assert.Equal(expectedAssemblies, actualAssemblies);
    }

    /// <summary>
    /// 通过特性获取Type
    /// </summary>
    [Fact]
    public void FindTypesByAttribute_ReturnsCorrectTypes()
    {
        // Arrange
        var expectedTypes = new[] { typeof(MyClass1), typeof(MyClass2) };

        // Act
        var actualTypes = AssemblyHelper.FindTypesByAttribute<TestAttribute>();

        // Assert
        Assert.Equal(expectedTypes, actualTypes);
    }

    /// <summary>
    /// 查找程序集
    /// </summary>
    [Fact]
    public void FindAllItems_ReturnsCorrectAssemblies()
    {
        // Arrange
        var expectedAssemblies = new[] { typeof(AssemblyHelper).Assembly };

        // Act
        var actualAssemblies = AssemblyHelper.FindAllItems(a => expectedAssemblies.Contains(a));

        // Assert
        Assert.Equal(expectedAssemblies, actualAssemblies);
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
    public string a { get; set; }
}

[Test]
file class MyClass2
{
    public string a { get; set; }
}