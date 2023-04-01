using EasilyNET.Core.ChineseLunar;
using EasilyNET.Core.Enums;
using EasilyNET.Core.Misc;
using FluentAssertions;

namespace EasilyNET.Tools.Test.Unit.ChineseLunar;

/// <summary>
/// 中国农历测试
/// </summary>
public class ChineseLunarTest
{
    /// <summary>
    /// 中国农历
    /// </summary>
    [Fact]
    public void ChineseLunar()
    {
        var date = "1994-11-15".ToDateTime();
        Lunar.Init(date);
        Lunar.Constellation.Should().Be(EConstellation.天蝎座);
        Lunar.Animal.Should().Be(EZodiac.狗);
        Lunar.ChineseLunar.Should().Be("一九九四年十月十三");
    }
}