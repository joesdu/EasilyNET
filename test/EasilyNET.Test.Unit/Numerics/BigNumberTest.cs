using System.Numerics;
using EasilyNET.Core.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }

    [TestMethod]
    public void TestSubtract()
    {
        var a = BigNumber.FromBigInteger(new(30));
        var b = BigNumber.FromBigInteger(new(20));
        var result = BigNumber.Subtract(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(10)));
    }

    [TestMethod]
    public void TestMultiply()
    {
        var a = BigNumber.FromBigInteger(new(10));
        var b = BigNumber.FromBigInteger(new(20));
        var result = BigNumber.Multiply(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(200)));
    }

    [TestMethod]
    public void TestDivide()
    {
        var a = BigNumber.FromBigInteger(new(20));
        var b = BigNumber.FromBigInteger(new(10));
        var result = BigNumber.Divide(a, b);
        result.ShouldBe(BigNumber.FromBigInteger(new(2)));
    }

    [TestMethod]
    public void TestPow()
    {
        var a = BigNumber.FromBigInteger(new(2));
        var result = BigNumber.Pow(a, new BigInteger(3));
        result.ShouldBe(BigNumber.FromBigInteger(new(8)));
    }

    [TestMethod]
    public void TestAbs()
    {
        var a = BigNumber.FromBigInteger(new(-10));
        var result = BigNumber.Abs(a);
        result.ShouldBe(BigNumber.FromBigInteger(new(10)));
    }
}