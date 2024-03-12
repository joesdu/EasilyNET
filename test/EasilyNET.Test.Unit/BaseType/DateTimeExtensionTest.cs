using EasilyNET.Core.Enums;
using EasilyNET.Core.Misc;

namespace EasilyNET.Test.Unit;

/// <summary>
/// </summary>
[TestClass]
public class DateTimeExtensionTest
{
    /// <summary>
    /// 测试获取任意时间的当天起始时间.
    /// </summary>
    [TestMethod]
    public void TestDayStartReturnsMidnight()
    {
        // Arrange
        var dateTime1 = new DateTime(2022, 1, 1, 12, 30, 0);
        // Act
        var dayStart1 = dateTime1.DayStart();
        // Assert
        Assert.AreEqual(new(2022, 1, 1, 0, 0, 0), dayStart1);

        // Arrange
        var dateTime2 = new DateTime(2022, 1, 1, 12, 30, 0);
        // Act
        var dayStart2 = dateTime2.DayStart();
        // Assert
        Assert.AreEqual(dateTime2.Date, dayStart2);

        // Arrange
        var dateTime3 = new DateTime(2024, 2, 29, 12, 30, 0);
        var dayStart3 = dateTime3.DayStart();
        Assert.AreEqual(new(2024, 2, 29, 0, 0, 0), dayStart3);
    }

    /// <summary>
    /// 时间重合测试
    /// </summary>
    [TestMethod]
    public void TestTimeOverlap()
    {
        // Test case 0: 完全重合
        var sub0 = Tuple.Create(new DateTime(2022, 1, 10), new DateTime(2022, 1, 20));
        var validate0 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.完全重合, DateTimeExtension.TimeOverlap(sub0, validate0));

        // Test case 1: 完全重合
        var sub1 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        var validate1 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.完全重合, DateTimeExtension.TimeOverlap(sub1, validate1));

        // Test case 2: 完全重合
        var sub2 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 15));
        var validate2 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.完全重合, DateTimeExtension.TimeOverlap(sub2, validate2));

        // Test case 3: 完全重合
        var sub3 = Tuple.Create(new DateTime(2022, 1, 15), new DateTime(2022, 1, 31));
        var validate3 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.完全重合, DateTimeExtension.TimeOverlap(sub3, validate3));

        // Test case 4: 完全不重合
        var sub4 = Tuple.Create(new DateTime(2022, 2, 1), new DateTime(2022, 2, 28));
        var validate4 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.完全不重合, DateTimeExtension.TimeOverlap(sub4, validate4));

        // Test case 5: 后段重合
        var sub5 = Tuple.Create(new DateTime(2022, 2, 1), new DateTime(2022, 2, 10));
        var validate5 = Tuple.Create(new DateTime(2022, 2, 5), new DateTime(2022, 2, 20));
        Assert.AreEqual(ETimeOverlap.后段重合, DateTimeExtension.TimeOverlap(sub5, validate5));

        // Test case 6: 前段重合
        var sub6 = Tuple.Create(new DateTime(2022, 2, 10), new DateTime(2022, 2, 15));
        var validate6 = Tuple.Create(new DateTime(2022, 2, 8), new DateTime(2022, 2, 14));
        Assert.AreEqual(ETimeOverlap.前段重合, DateTimeExtension.TimeOverlap(sub6, validate6));
    }
}
