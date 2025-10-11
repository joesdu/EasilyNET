using EasilyNET.Core.Misc;

namespace EasilyNET.Test.Unit.Misc;

/// <summary>
/// 对象帮助类测试
/// </summary>
[TestClass]
public class ObjectExtension
{
    /// <summary>
    /// 测试设置Name属性结果等于true
    /// </summary>
    [TestMethod]
    public void TestTrySetPropertyNameWhenTrue()
    {
        var person = new Person();
        var succeed = person.TrySetProperty(o => o.Name, () => "大黄瓜");
        Assert.IsTrue(succeed);
        Assert.AreEqual("大黄瓜", person.Name);
    }

    /// <summary>
    /// 测试设置Age属性，并修改原来值
    /// </summary>
    [TestMethod]
    public void TestTrySetPropertyAgeWithModifyOriginalValueWhenTrue()
    {
        var person = new Person
        {
            Age = 1
        };
        var succeed = person.TrySetProperty(o => o.Age, value => value.Age + 1);
        Assert.IsTrue(succeed);
        Assert.AreEqual(2, person.Age);
    }

    /// <summary>
    /// 测试设置Time属性,等于Null
    /// </summary>
    [TestMethod]
    public void TestTrySetPropertyTimeWhenNull()
    {
        var person = new Person
        {
            Time = DateTime.Now
        };
        Assert.IsTrue(person.Time.HasValue);
        var succeed = person.TrySetProperty(o => o.Time, () => null);
        Assert.IsTrue(succeed);
        Assert.IsNull(person.Time);
    }
}

public class Person
{
    public string? Name { get; set; }

    public int Age { get; set; }

    public DateTime? Time { get; set; }
}