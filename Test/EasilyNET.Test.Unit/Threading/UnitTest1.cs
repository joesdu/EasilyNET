using EasilyNET.Core.Threading;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Threading;

/// <summary>
/// 测试异步锁
/// </summary>
[TestClass]
public class UnitTest1
{

    private static  Dictionary<string, string> _dictionary = new ();
    /// <summary>
    /// 测试异步锁
    /// </summary>
    [TestMethod]
    public async Task TestAsyncLock()
    {

        AsyncLock asyncLock = new AsyncLock();
        Parallel.For(0, 1000, async i =>
        {
        
            int k = i;
           
            //不会并发冲突
            using (await  asyncLock.LockAsync())
            {
               await  Task.Run(()=>_dictionary.Add(k.ToString(), k.ToString()));
            }
        });

    }
    
   
}