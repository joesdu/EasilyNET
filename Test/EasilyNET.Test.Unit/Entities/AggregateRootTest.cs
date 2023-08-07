using EasilyNET.Core.Entities;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Entities;

/// <summary>
/// 测试
/// </summary>
[TestClass]
public class AggregateRoot_Test
{
    /// <summary>
    /// AggregateRootTest
    /// </summary>
    [TestMethod]
    public void AggregateRootTest()
    {
        new Test().Equals(new Test()).Should().BeFalse();
        var test1 = new Test(1, "大黄瓜", 18);
        var test2 = new Test(1, "大黄瓜", 18);
        test1.Equals(test2).Should().BeTrue();
        var test3 = new Test(2, "少妇", 35);
        Assert.IsFalse(test2.Equals(test3));
        test3.UpdateName("离异带娃");
        Assert.IsTrue(test3.Equals(test3));
    }

    /// <summary>
    /// AggregateRoot_Guid_Test
    /// </summary>
    [TestMethod]
    public void AggregateRoot_Guid_Test()
    {
        var value1 = Guid.NewGuid();
        var value2 = Guid.NewGuid();
        Assert.IsTrue(new Person(value1).Equals(new Person(value1)));
        Assert.IsFalse(new Person(value1).Equals(new Person(value2)));
        Assert.IsFalse(new Person().Equals(new Person(value1)));
        Assert.IsFalse(new Person().Equals(new Person()));
    }

    private sealed class Test : FullAggregateRoot<long, long>
    {
        public Test() { }

        public Test(long id, string name, int age)
        {
            Name = name;
            Age = age;
            Id = id;
            IsDelete = false;
        }

        public string Name { get; private set; }

        public int Age { get; private set; }

        public void UpdateName(string name) => Name = name;

        public void InitCreateTime() => CreateTime = DateTime.Now;

        public void InitUpdateTime() => UpdatedTime = DateTime.Now;
        public void Delete() => IsDelete = true;
    }

    private sealed class Person : AggregateRoot<Guid>
    {
        public Person() { }

        public Person(Guid id)
        {
            Id = id;
        }
    }
}