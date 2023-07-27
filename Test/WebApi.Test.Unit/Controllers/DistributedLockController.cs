using EasilyNET.MongoDistributedLock.Attributes;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// MongoDB分布式锁测试
/// </summary>
[Route("api/[controller]/[action]"), ApiController, ApiGroup("Distributed", "v1", "分布式锁测试")]
public class DistributedLockController(IDistributedLock mongoLock) : ControllerBase
{
    /// <summary>
    /// AcquireLock
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> AcquireLock()
    {
        var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
        return new
        {
            锁ID = acq.AcquireId.ToString(),
            实际值 = acq.Acquired,
            预期值 = true
        };
    }

    /// <summary>
    /// Acquire_And_Block
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> Acquire_And_Block()
    {
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        return new
        {
            锁ID1 = acq1.AcquireId.ToString(),
            实际值1 = acq1.Acquired,
            预期值1 = true,
            实际值2 = acq2.Acquired,
            预期值2 = false
        };
    }
}