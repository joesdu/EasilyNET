using EasilyNET.Core.Enums;
using EasilyNET.Extensions;
using FluentAssertions;

namespace EasilyNET.Tools.Test.Unit.ChineseLunar;

public class ChineseLunarTest
{
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