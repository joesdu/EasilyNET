using FluentAssertions;

namespace EasilyNET.Tests.Unit;

/// <summary>
/// ReferenceItem 测试
/// </summary>
[TestClass]
public class ReferenceItemTest
{
    /// <summary>
    /// Equals Test
    /// </summary>
    [TestMethod]
    public void ReferenceItemEquals()
    {
        // Arrange
        ReferenceItem o1 = new("124124", "李四");
        ReferenceItem o2 = new("124124", "李四");
        ReferenceItem o3 = new("124124", "王五");

        // Act
        var r1 = o1.Equals(o2);
        var r2 = o1 == o2;
        var r3 = o1 != o3;

        // Assert
        r1.Should().Be(true);
        r2.Should().Be(true);
        r3.Should().Be(true);
    }
}