using EasilyNET.Core.BaseType;
using FluentAssertions;
using System.Diagnostics;

namespace EasilyNET.Test.Unit.BaseType;

/// <summary>
/// 测试雪花ID,只支持long类型
/// </summary>
[TestClass]
public class SnowFlakeIdTest
{
    /// <summary>
    /// </summary>
    [TestMethod]
    public void TestDefaultNextId()
    {
        var id = SnowFlakeId.Default.NextId();
        id.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 检测ID是不是递增的
    /// </summary>
    [TestMethod]
    public void TestGenerateIncreasing_Ids()
    {
        var lastId = 0L;
        for (var i = 0; i < 100; i++)
        {
            var id = SnowFlakeId.Default.NextId();
            id.Should().BeGreaterThan(lastId);
            lastId = id;
        }
    }

    /// <summary>
    /// 生成唯一ID测试
    /// </summary>
    [TestMethod]
    public void GenerateOnlyUniqueIds()
    {
        var set = new HashSet<long>();
        const int N = 2000000;
        for (var i = 0; i < N; i++)
        {
            var id = SnowFlakeId.Default.NextId();
            if (set.Contains(id))
            {
                Debug.WriteLine($"重复ID{id}");
            }
            else
            {
                set.Add(id);
            }
        }
        set.Count.Should().Be(N);
    }

    /// <summary>
    /// Task生成唯一ID测试
    /// </summary>
    [TestMethod]
    public void GenerateOnlyUniqueTaskIds()
    {
        var set = new HashSet<long>();
        const int N = 2000000;
        var lockObject = new object();
        const int numberOfThreads = 10;
        Parallel.For(0, numberOfThreads, i =>
        {
            for (var j = 0; j < N; j++)
            {
                var id = SnowFlakeId.Default.NextId();
                lock (lockObject)
                {
                    if (set.Contains(id))
                    {
                        Debug.WriteLine($"重复ID{id}");
                    }
                    else
                    {
                        set.Add(id);
                    }
                }
            }
        });
        set.Count.Should().Be(N * numberOfThreads);
    }

    /// <summary>
    /// </summary>
    [TestMethod]
    public void It_should_properly_mask_worker_id()
    {
        // Arrange
        const int workerId = 123;
        SnowFlakeId.SetDefaultSnowFlakeId(new(workerId));
        const long expectedMaskedWorkerId = workerId & 0xFFF; // 0xFFF is the mask for 12 bits

        // Act
        var generatedId = SnowFlakeId.Default.NextId();
        var maskedWorkerId = (generatedId >> 10) & 0xFFF; // Shift and mask to get workerId

        // Assert
        Assert.AreEqual(expectedMaskedWorkerId, maskedWorkerId);
    }
}
