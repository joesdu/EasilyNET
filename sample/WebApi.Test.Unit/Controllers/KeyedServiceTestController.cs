using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Swaggers.Attributes;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[Route("api/[controller]")]
[ApiController]
[ApiGroup("KeyedServiceTest", "KeyedServiceTestController")]
public class KeyedServiceTestController(IServiceProvider sp, [FromKeyedServices("helloKey")] IKeyedServiceTest2 kst2) : ControllerBase
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
    public string ShowHello2() => kst2.ShowHello2();

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
[DependencyInjection(ServiceLifetime.Transient, AsType = typeof(IKeyedServiceTest2), ServiceKey = "helloKey")]
public sealed class KeyedServiceTest : IKeyedServiceTest, IKeyedServiceTest2
{
    /// <inheritdoc />
    public string ShowHello() => "Hello, KeyedServiceTest!";

    /// <inheritdoc />
    public string ShowHello2() => "Hello2, KeyedServiceTest!";
}

/// <summary>
/// IKeyedServiceTest2
/// </summary>
public interface IKeyedServiceTest2
{
    /// <summary>
    /// ShowHello2
    /// </summary>
    /// <returns></returns>
    string ShowHello2();
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