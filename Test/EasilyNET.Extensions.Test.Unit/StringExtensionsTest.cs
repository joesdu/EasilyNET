using FluentAssertions;

namespace EasilyNET.Extensions.Test.Unit;

/// <summary>
/// </summary>
public class StringExtensionsTest
{
    /// <summary>
    /// </summary>
    [SetUp]
    public void Setup() { }

    /// <summary>
    /// Truncate Test
    /// </summary>
    /// <param name="value"></param>
    [TestCase("12345678910"), TestCase("1234567891011"), TestCase("12345678910111213141516")]
    public void TestTruncate(string value)
    {
        // 当suffix长度大于希望的最大长度
        const string suffix = "........";
        var result1 = value.Truncate(7, suffix);
        result1.Length.Should().Be(7);
        result1.Should().Be(suffix[..7]);
        // 当suffix长度等于希望的最大长度
        var result2 = value.Truncate(8, suffix);
        result2.Length.Should().Be(8);
        result2.Should().Be("........");
        // 当suffix长度小于希望的最大长度
        var result3 = value.Truncate(11, suffix);
        switch (value)
        {
            case "12345678910":
                var leg = value.Length <= 11 ? value.Length : 11;
                result3.Length.Should().Be(leg);
                result3.Should().Be("12345678910");
                break;
            case "1234567891011":
                result3.Length.Should().Be(11);
                result3.Should().Be("123........");
                break;
            case "12345678910111213141516":
                result3.Length.Should().Be(11);
                result3.Should().Be("123........");
                break;
        }
        Assert.Pass();
    }
}