using EasilyNET.Core.BaseType;
using FluentAssertions;
using System.Collections.Concurrent;
using Yitter.IdGenerator;

namespace EasilyNET.Test.Unit.BaseType;

/// <summary>
/// 测试雪花ID,只支持long类型
/// </summary>
[TestClass]
public class SnowFlakeIdTest
{
    /// <summary>
    ///
    /// </summary>
    [TestMethod]
    public void TestDefaultNextId()
    {
        var id = SnowFlakeId.Default.NextId();
        var id1 = SnowFlakeId.Default.NextId();
        var equal = id == id1;
        equal.Should().BeFalse();
    }
    
    /// <summary>
    ///
    /// </summary>
    [TestMethod]
    public void TestForNextId()
    {
        List<long> list = new List<long>();
        for (int i = 0; i < 100; i++)
        {
            list.Add(SnowFlakeId.Default.NextId());
        }
        var equal= list.Distinct().Count()==list.Count();
        equal.Should().Be(true);
    }
    
    /// <summary>
    ///
    /// </summary>
    [TestMethod]
    public void TestSetIdGeneratorWithDefaultNextId()
    {
        var id= SnowFlakeId.Default.NextId();
        SnowFlakeId.SetIdGenerator(new IdGeneratorOptions(2));
        var id2=  SnowFlakeId.Default.NextId();
       var equal= id == id2;
        equal.Should().BeFalse();
    }

    /// <summary>
    /// 测试是否多线程安全
    /// </summary>
    [TestMethod]
    public async void TestForTaskNextId()
    {

        var blockingCollection = new BlockingCollection<long>();
        List<Task> tasks = new List<Task>();
        for (int i = 0; i < 100 ; i++)
        {
            var task = Task.Run(() =>
            {

                for (int j = 0; j < 100; j++)
                {

                    blockingCollection.Add(SnowFlakeId.Default.NextId());
                }
            }); 
            tasks.Add(task);
        }

        await Task.WhenAll(tasks.ToArray());
        var equal= blockingCollection.Distinct().Count() == tasks.Count();
        equal.Should().BeTrue();
    }


}