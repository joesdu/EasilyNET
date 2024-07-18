using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// KeyedServiceTest
/// </summary>
[Route("api/[controller]"), ApiController, ApiGroup("KeyedServiceTest", "v1", "KeyedServiceTestController")]
public class KeyedServiceTestController(IServiceProvider sp, IKeyedServiceTest kst2) : ControllerBase
{
    /// <summary>
    /// ShowHello
    /// </summary>
    /// <returns></returns>
    [HttpGet("HelloKeyedService")]
    public string ShowHello()
    {
        var kst = sp.GetRequiredKeyedService<KeyedServiceTest>("helloKey");
        return kst.ShowHello();
    }

    /// <summary>
    /// ShowHello2
    /// </summary>
    /// <returns></returns>
    [HttpGet("HelloKeyedService2")]
    public string ShowHello2() => kst2.ShowHello();

    /// <summary>
    /// ShowHello3
    /// </summary>
    /// <returns></returns>
    [HttpGet("HelloKeyedService3")]
    public string ShowHello3()
    {
        var kst = sp.GetRequiredKeyedService<IKeyedServiceTest>("helloKey");
        return kst.ShowHello();
    }
}

/// <summary>
/// KeyedServiceTest
/// </summary>
[DependencyInjection(ServiceLifetime.Transient, ServiceKey = "helloKey")]
public sealed class KeyedServiceTest : IKeyedServiceTest
{
    /// <summary>
    /// ShowHello
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string ShowHello() => "Hello, KeyedServiceTest!";
}

/// <summary>
/// IKeyedServiceTest
/// </summary>
public interface IKeyedServiceTest
{
    /// <summary>
    /// ShowHello
    /// </summary>
    /// <returns></returns>
    string ShowHello();
}