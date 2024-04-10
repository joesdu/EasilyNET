using EasilyNET.Core.Misc;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Misc;

/// <summary>
/// RmbCapitalizedTest
/// </summary>
[TestClass]
public class RmbCapitalizedTest
{
    /// <summary>
    /// 较小值
    /// </summary>
    [TestMethod, DataRow(1594.6589)]
    public void RmbCapitalizedMin(double value)
    {
        Console.WriteLine(value.ToRmb());
        value.ToRmb().Should().Be("壹仟伍佰玖拾肆元陆角陆分");
    }

    /// <summary>
    /// 较大值
    /// </summary>
    [TestMethod, DataRow("1594278327421378518276358712.6589")]
    public void RmbCapitalizedMax(string value)
    {
        Console.WriteLine(value.ToRmb());
        value.ToRmb().Should().Be("壹仟伍佰玖拾肆秭贰仟柒佰捌拾叁垓贰仟柒佰肆拾贰京壹仟叁佰柒拾捌兆伍仟壹佰捌拾贰亿柒仟陆佰叁拾伍万捌仟柒佰壹拾贰元柒角");
    }
}