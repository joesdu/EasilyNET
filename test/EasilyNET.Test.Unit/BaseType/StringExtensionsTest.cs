using EasilyNET.Core.Misc;
using FluentAssertions;

namespace EasilyNET.Core.Test.Unit;

/// <summary>
/// </summary>
[TestClass]
public class StringExtensionsTest
{
    /// <summary>
    /// Truncate Test
    /// </summary>
    [TestMethod]
    public void TestTruncate()
    {
        // 当suffix长度大于希望的最大长度
        const string suffix = "........";
        var value = "12345678910";
        var result1 = value.Truncate(7, suffix);
        result1.Length.Should().Be(7);
        result1.Should().Be(suffix[..7]);
        // 当suffix长度等于希望的最大长度
        value = "1234567891011";
        var result2 = value.Truncate(8, suffix);
        result2.Length.Should().Be(8);
        result2.Should().Be("........");
        // 当suffix长度小于希望的最大长度
        value = "12345678910111213141516";
        var result3 = value.Truncate(11, suffix);
        result3.Length.Should().Be(11);
        result3.Should().Be("123........");
    }
}
