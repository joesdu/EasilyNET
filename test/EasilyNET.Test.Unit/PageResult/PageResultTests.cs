namespace EasilyNET.Test.Unit.PageResult;

/// <summary>
/// 分页返回测试
/// </summary>
[TestClass]
public class PageResultTests
{
    /// <summary>
    /// 分页返回数组
    /// </summary>
    [TestMethod]
    public void Wrap_ReturnsPageResultWithCorrectTotalAndList()
    {
        // Arrange
        const long total = 3L;
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = Core.PageResult.Wrap(total, list);

        // Assert
        Assert.AreEqual(total, result.Total);
        Assert.AreEqual(list, result.List);
    }

    /// <summary>
    /// 分页返回动态类型
    /// </summary>
    [TestMethod]
    public void WrapDynamic_ReturnsPageResultWithCorrectTotalAndList()
    {
        // Arrange
        const long total = 3L;
        var list = new List<dynamic> { 1, "hello", true };

        // Act
        var result = Core.PageResult.WrapDynamic(total, list);

        // Assert
        Assert.AreEqual(total, result.Total);
        Assert.AreEqual(list, result.List);
    }

    /// <summary>
    /// 测试total为空
    /// </summary>
    [TestMethod]
    public void Wrap_ReturnsPageResultWithZeroTotal_WhenNullTotalIsPassed()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };
        long? total = null;

        // Act
        var result = Core.PageResult.Wrap(total, list);

        // Assert
        Assert.AreEqual(0, result.Total);
        Assert.AreEqual(list, result.List);
    }

    /// <summary>
    /// 测试数据列表为空
    /// </summary>
    [TestMethod]
    public void Wrap_ReturnsPageResultWithEmptyList_WhenNullListIsPassed()
    {
        // Arrange
        IEnumerable<string>? list = null;
        const long total = 3L;

        // Act
        var result = Core.PageResult.Wrap(total, list);

        // Assert
        Assert.AreEqual(total, result.Total);
        Assert.IsNull(result.List);
    }

    /// <summary>
    /// 测试total为空和动态类型
    /// </summary>
    [TestMethod]
    public void WrapDynamic_ReturnsPageResultWithZeroTotal_WhenNullTotalIsPassed()
    {
        // Arrange
        var list = new List<dynamic> { "a", 1, true };
        long? total = null;

        // Act
        var result = Core.PageResult.WrapDynamic(total, list);

        // Assert
        Assert.AreEqual(0, result.Total);
        Assert.AreEqual(list, result.List);
    }

    /// <summary>
    /// 测试动态数据为空
    /// </summary>
    [TestMethod]
    public void WrapDynamic_ReturnsPageResultWithEmptyList_WhenNullListIsPassed()
    {
        // Arrange
        IEnumerable<dynamic>? list = null;
        const long total = 3L;

        // Act
        var result = Core.PageResult.WrapDynamic(total, list);

        // Assert
        Assert.AreEqual(total, result.Total);
        Assert.IsNull(result.List);
    }
}