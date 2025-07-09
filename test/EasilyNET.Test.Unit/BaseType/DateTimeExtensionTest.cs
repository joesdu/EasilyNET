using EasilyNET.Core.Enums;
using EasilyNET.Core.Misc;

namespace EasilyNET.Test.Unit.BaseType;

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
        var dayStart1 = dateTime1.DayStart;
        // Assert
        Assert.AreEqual<DateTime>(new(2022, 1, 1, 0, 0, 0), dayStart1);

        // Arrange
        var dateTime2 = new DateTime(2022, 1, 1, 12, 30, 0);
        // Act
        var dayStart2 = dateTime2.DayStart;
        // Assert
        Assert.AreEqual(dateTime2.Date, dayStart2);

        // Arrange
        var dateTime3 = new DateTime(2024, 2, 29, 12, 30, 0);
        var dayStart3 = dateTime3.DayStart;
        Assert.AreEqual<DateTime>(new(2024, 2, 29, 0, 0, 0), dayStart3);
    }

    /// <summary>
    /// 时间重合测试
    /// </summary>
    [TestMethod]
    public void TestTimeOverlap()
    {
        // Test case 0: Sub within Source (完全重合 in old enum)
        var sub0 = Tuple.Create(new DateTime(2022, 1, 10), new DateTime(2022, 1, 20));
        var source0 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.SubWithinSource, DateTimeExtensions.TimeOverlap(sub0, source0));

        // Test case 1: Sub within Source (Exact match - sub is same as source)
        var sub1 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        var source1 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.SubWithinSource, DateTimeExtensions.TimeOverlap(sub1, source1)); // Or SourceWithinSub, depending on strict definition. Current logic makes it SubWithinSource.

        // Test case 2: Sub within Source (Sub starts at source start)
        var sub2 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 15));
        var source2 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.SubWithinSource, DateTimeExtensions.TimeOverlap(sub2, source2));

        // Test case 3: Sub within Source (Sub ends at source end)
        var sub3 = Tuple.Create(new DateTime(2022, 1, 15), new DateTime(2022, 1, 31));
        var source3 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.SubWithinSource, DateTimeExtensions.TimeOverlap(sub3, source3));

        // Test case 4: No overlap (Sub after source)
        var sub4 = Tuple.Create(new DateTime(2022, 2, 1), new DateTime(2022, 2, 28));
        var source4 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.NoOverlap, DateTimeExtensions.TimeOverlap(sub4, source4));

        // Test case 5: Sub overlaps end of Source (后段重合 in old enum)
        var sub5 = Tuple.Create(new DateTime(2022, 2, 5), new DateTime(2022, 2, 20));    // sub: Feb 5 - Feb 20
        var source5 = Tuple.Create(new DateTime(2022, 2, 1), new DateTime(2022, 2, 10)); // source: Feb 1 - Feb 10
        Assert.AreEqual(ETimeOverlap.SubOverlapsEndOfSource, DateTimeExtensions.TimeOverlap(sub5, source5));

        // Test case 6: Sub overlaps start of Source (前段重合 in old enum)
        var sub6 = Tuple.Create(new DateTime(2022, 2, 8), new DateTime(2022, 2, 14));     // sub: Feb 8 - Feb 14
        var source6 = Tuple.Create(new DateTime(2022, 2, 10), new DateTime(2022, 2, 15)); // source: Feb 10 - Feb 15
        Assert.AreEqual(ETimeOverlap.SubOverlapsStartOfSource, DateTimeExtensions.TimeOverlap(sub6, source6));

        // Test case 7: Source within Sub
        var sub7 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        var source7 = Tuple.Create(new DateTime(2022, 1, 10), new DateTime(2022, 1, 20));
        Assert.AreEqual(ETimeOverlap.SourceWithinSub, DateTimeExtensions.TimeOverlap(sub7, source7));

        // Test case 8: No overlap (Sub before source)
        var sub8 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 15));
        var source8 = Tuple.Create(new DateTime(2022, 1, 20), new DateTime(2022, 1, 31));
        Assert.AreEqual(ETimeOverlap.NoOverlap, DateTimeExtensions.TimeOverlap(sub8, source8));

        // Test case 9: Sub overlaps start of Source (Sub ends within source)
        var sub9 = Tuple.Create(new DateTime(2022, 3, 1), new DateTime(2022, 3, 15));     // sub: Mar 1 - Mar 15
        var source9 = Tuple.Create(new DateTime(2022, 3, 10), new DateTime(2022, 3, 20)); // source: Mar 10 - Mar 20
        Assert.AreEqual(ETimeOverlap.SubOverlapsStartOfSource, DateTimeExtensions.TimeOverlap(sub9, source9));

        // Test case 10: Sub overlaps end of Source (Sub starts within source)
        var sub10 = Tuple.Create(new DateTime(2022, 3, 10), new DateTime(2022, 3, 25));   // sub: Mar 10 - Mar 25
        var source10 = Tuple.Create(new DateTime(2022, 3, 5), new DateTime(2022, 3, 15)); // source: Mar 5 - Mar 15
        Assert.AreEqual(ETimeOverlap.SubOverlapsEndOfSource, DateTimeExtensions.TimeOverlap(sub10, source10));

        // Test case 11: ArgumentException for sub
        var sub11 = Tuple.Create(new DateTime(2022, 1, 20), new DateTime(2022, 1, 10)); // Invalid sub
        var source11 = Tuple.Create(new DateTime(2022, 1, 1), new DateTime(2022, 1, 31));
        Assert.Throws<ArgumentException>(() => DateTimeExtensions.TimeOverlap(sub11, source11));

        // Test case 12: ArgumentException for source
        var sub12 = Tuple.Create(new DateTime(2022, 1, 10), new DateTime(2022, 1, 20));
        var source12 = Tuple.Create(new DateTime(2022, 1, 31), new DateTime(2022, 1, 1)); // Invalid source
        Assert.Throws<ArgumentException>(() => DateTimeExtensions.TimeOverlap(sub12, source12));

        // Test case 13: No overlap (sub ends exactly at source start)
        var sub13 = Tuple.Create(new DateTime(2023, 1, 1), new DateTime(2023, 1, 5));
        var source13 = Tuple.Create(new DateTime(2023, 1, 5), new DateTime(2023, 1, 10));
        Assert.AreEqual(ETimeOverlap.NoOverlap, DateTimeExtensions.TimeOverlap(sub13, source13));

        // Test case 14: No overlap (sub starts exactly at source end)
        var sub14 = Tuple.Create(new DateTime(2023, 1, 10), new DateTime(2023, 1, 15));
        var source14 = Tuple.Create(new DateTime(2023, 1, 5), new DateTime(2023, 1, 10));
        Assert.AreEqual(ETimeOverlap.NoOverlap, DateTimeExtensions.TimeOverlap(sub14, source14));
    }
}