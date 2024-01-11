namespace EasilyNET.Core.Helpers.Tests;

/// <summary>
/// 对象帮助类测试
/// </summary>
[TestClass]
public class ObjectHelperTests
{
    /// <summary>
    /// 测试设置Name属性结果等于true
    /// </summary>
    [TestMethod]
    public void TestTrySetPropertyNameWhenTrue()
    {
        var person = new Person();
        ObjectHelper.TrySetProperty(person, o => o.Name, () => "大黄瓜");
        Assert.AreEqual(person.Name, "大黄瓜");
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
        ObjectHelper.TrySetProperty(person, o => o.Age, value => value.Age + 1);
        Assert.AreEqual(person.Age, 2);
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
        ObjectHelper.TrySetProperty(person, o => o.Time, () => null);
        Assert.AreEqual(person.Time, null);
    }
}

public class Person
{
    public string Name { get; set; }

    public int Age { get; set; }

    public DateTime? Time { get; set; }
}