using System.Diagnostics;
using EasilyNET.Core.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace EasilyNET.Test.Unit.System;

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
        id.ShouldBeGreaterThan(0);
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
            id.ShouldBeGreaterThan(lastId);
            lastId = id;
        }
    }

    /// <summary>
    /// 生成唯一ID测试
    /// </summary>
    [TestMethod]
    [Ignore]
    public void GenerateOnlyUniqueIds()
    {
        var set = new HashSet<long>();
        const int N = 2000000;
        for (var i = 0; i < N; i++)
        {
            var id = SnowFlakeId.Default.NextId();
            if (!set.Add(id))
            {
                Debug.WriteLine($"重复ID{id}");
            }
        }
        set.Count.ShouldBe(N);
    }

    /// <summary>
    /// Task生成唯一ID测试
    /// </summary>
    [TestMethod]
    [Ignore]
    public void GenerateOnlyUniqueTaskIds()
    {
        var set = new HashSet<long>();
        const int N = 2000000;
        var lockObject = new Lock();
        const int numberOfThreads = 10;
        Parallel.For(0, numberOfThreads, _ =>
        {
            for (var j = 0; j < N; j++)
            {
                var id = SnowFlakeId.Default.NextId();
                lock (lockObject)
                {
                    if (!set.Add(id))
                    {
                        Debug.WriteLine($"重复ID{id}");
                    }
                }
            }
        });
        set.Count.ShouldBe(N * numberOfThreads);
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