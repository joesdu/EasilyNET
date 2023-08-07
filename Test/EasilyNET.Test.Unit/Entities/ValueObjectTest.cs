using EasilyNET.Core.Entities;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Entities;

/// <summary>
/// 测试值对象
/// </summary>
[TestClass]
public class ValueObjectTest
{
    /// <summary>
    /// 测试是否相等
    /// </summary>
    [TestMethod]
    public void TestValueObjectWhenTrue()
    {
        var valueObject1 = new ValueObject_Test(10, "大黄瓜", "18cm");
        var valueObject2 = new ValueObject_Test(10, "大黄瓜", "18cm");
        valueObject1.ValueEquals(valueObject2).Should().BeTrue();
    }

    /// <summary>
    /// 测试是否不相等
    /// </summary>
    [TestMethod]
    public void TestValueObjectWhenNotTrue()
    {
        var valueObject1 = new ValueObject_Test(10, "大黄瓜", "18cm");
        var valueObject2 = new ValueObject_Test(20, "大黄瓜", "真猛");
        valueObject1.ValueEquals(valueObject2).Should().BeFalse();
        var valueObject3 = new ValueObject_Test(20, "大黄瓜", "20CM");
        valueObject2.ValueEquals(valueObject3).Should().BeFalse();
        var valueObject4 = new ValueObject_Test(20, null!, "20CM");
        valueObject3.ValueEquals(valueObject4).Should().BeFalse();
    }

    private sealed class ValueObject_Test : ValueObject
    {
        public ValueObject_Test(int a, string b, string c)
        {
            A = a;
            B = b;
            C = c;
        }

        public int A { get; }

        public string B { get; }

        public string C { get; }

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }
}