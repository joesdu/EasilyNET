using System.Numerics;
using EasilyNET.Core.Numerics;
using Shouldly;

namespace EasilyNET.Test.Unit.Numerics;

[TestClass]
public class BigNumberTest
{
    [TestMethod]
    public void TestAdd()
    {
        var a = BigNumber.FromBigInteger(new(10));
        var b = BigNumber.FromBigInteger(new(20));
        var result = BigNumber.Add(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(30)));
        // 分数加法
        var f1 = BigNumber.FromBigInteger(new(1), new(2));
        var f2 = BigNumber.FromBigInteger(new(1), new(3));
        var fsum = BigNumber.Add(f1, f2);
        fsum.ShouldBe(BigNumber.FromBigInteger(new(5), new(6)));
    }

    [TestMethod]
    public void TestSubtract()
    {
        var a = BigNumber.FromBigInteger(new(30));
        var b = BigNumber.FromBigInteger(new(20));
        var result = BigNumber.Subtract(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(10)));
        // 分数减法
        var f1 = BigNumber.FromBigInteger(new(3), new(4));
        var f2 = BigNumber.FromBigInteger(new(1), new(2));
        var fdiff = BigNumber.Subtract(f1, f2);
        fdiff.ShouldBe(BigNumber.FromBigInteger(new(1), new(4)));
    }

    [TestMethod]
    public void TestMultiply()
    {
        var a = BigNumber.FromBigInteger(new(10));
        var b = BigNumber.FromBigInteger(new(20));
        var result = BigNumber.Multiply(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(200)));
        // 分数乘法
        var f1 = BigNumber.FromBigInteger(new(2), new(3));
        var f2 = BigNumber.FromBigInteger(new(3), new(5));
        var fprod = BigNumber.Multiply(f1, f2);
        fprod.ShouldBe(BigNumber.FromBigInteger(new(2 * 3), new(3 * 5)));
    }

    [TestMethod]
    public void TestDivide()
    {
        var a = BigNumber.FromBigInteger(new(20));
        var b = BigNumber.FromBigInteger(new(10));
        var result = BigNumber.Divide(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(2)));
        // 分数除法
        var f1 = BigNumber.FromBigInteger(new(3), new(4));
        var f2 = BigNumber.FromBigInteger(new(2), new(5));
        var fdiv = BigNumber.Divide(f1, f2);
        fdiv.ShouldBe(BigNumber.FromBigInteger(new(15), new(8)));
    }

    [TestMethod]
    public void TestPow()
    {
        var a = BigNumber.FromBigInteger(new(2));
        var result = BigNumber.Pow(a, new BigInteger(3));
        result.ShouldBe(BigNumber.FromBigInteger(new(8)));
        // 分数幂
        var f = BigNumber.FromBigInteger(new(2), new(3));
        var fpow = BigNumber.Pow(f, 2);
        fpow.ShouldBe(BigNumber.FromBigInteger(new(4), new(9)));
    }

    [TestMethod]
    public void TestAbs()
    {
        var a = BigNumber.FromBigInteger(new(-10));
        var result = BigNumber.Abs(a);
        result.ShouldBe(BigNumber.FromBigInteger(new(10)));
        // 分数绝对值
        var f = BigNumber.FromBigInteger(new(-2), new(3));
        var fabs = BigNumber.Abs(f);
        fabs.ShouldBe(BigNumber.FromBigInteger(new(2), new(3)));
    }

    [TestMethod]
    public void TestShiftOperators()
    {
        // 整数移位
        var a = BigNumber.FromBigInteger(new(3));
        (a << 2).ShouldBe(BigNumber.FromBigInteger(new(12)));
        (a >> 1).ShouldBe(BigNumber.FromBigInteger(new(1)));
        // 分数移位
        var f = BigNumber.FromBigInteger(new(3), new(4));
        (f << 2).ShouldBe(BigNumber.FromBigInteger(new(12), new(4)));
        (f >> 1).ShouldBe(BigNumber.FromBigInteger(new(1), new(4)));
    }

    [TestMethod]
    public void TestXorOperator()
    {
        // 整数异或
        var a = BigNumber.FromBigInteger(new(6));
        var b = BigNumber.FromBigInteger(new(3));
        (a ^ b).ShouldBe(BigNumber.FromBigInteger(new(5)));
        // 分数异或
        var f1 = BigNumber.FromBigInteger(new(1), new(2));
        var f2 = BigNumber.FromBigInteger(new(1), new(3));
        var fxor = f1 ^ f2;
        // 1/2 = 3/6, 1/3 = 2/6, 3^2=1, 1/6
        fxor.ShouldBe(BigNumber.FromBigInteger(new(1), new(6)));
    }

    [TestMethod]
    public void TestMod()
    {
        var a = BigNumber.FromBigInteger(new(17));
        var b = BigNumber.FromBigInteger(new(5));
        (a % b).ShouldBe(BigNumber.FromBigInteger(new(2)));
        // 分数取模
        var f1 = BigNumber.FromBigInteger(new(7), new(3)); // 2又1/3
        var f2 = BigNumber.FromBigInteger(new(1), new(2));
        var fmod = f1 % f2;
        // 2又1/3 = 7/3, 1/2 = 1/2, 7/3 % 1/2 = 1/3
        fmod.ShouldBe(BigNumber.FromBigInteger(new(1), new(3)));
    }

    [TestMethod]
    public void TestEqualsAndCompare()
    {
        var a = BigNumber.FromBigInteger(new(2), new(4));
        var b = BigNumber.FromBigInteger(new(1), new(2));
        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
        (a < BigNumber.FromBigInteger(new(3), new(4))).ShouldBeTrue();
        (a > BigNumber.FromBigInteger(new(1), new(4))).ShouldBeTrue();
        (a <= b).ShouldBeTrue();
        (a >= b).ShouldBeTrue();
    }
}