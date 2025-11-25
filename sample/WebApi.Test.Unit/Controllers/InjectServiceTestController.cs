using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[Route("api/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "InjectServiceTest")]
public class InjectServiceTestController(IServiceProvider sp) : ControllerBase
{
    /// <summary>
    /// ShowHello
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public string? ShowHello()
    {
        var kst = sp.GetService<InjectServiceTest>();
        return kst?.ShowHello();
    }

    /// <summary>
    /// ShowHello2
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public string? ShowHello2()
    {
        var kst2 = sp.GetService<IInjectServiceTest2>();
        return kst2?.ShowHello2();
    }

    /// <summary>
    /// ShowHello3
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public string? ShowHello3()
    {
        var kst = sp.GetService<IInjectServiceTest>();
        return kst?.ShowHello();
    }
}

/// <inheritdoc cref="IInjectServiceTest" />
[DependencyInjection(ServiceLifetime.Transient, AddSelf = true)]
public sealed class InjectServiceTest : IInjectServiceTest, IInjectServiceTest2
{
    /// <inheritdoc />
    public string ShowHello() => "Hello, Test!";

    /// <inheritdoc />
    public string ShowHello2() => "Hello2, Test!";
}

/// <summary>
/// </summary>
public interface IInjectServiceTest2
{
    /// <summary>
    /// ShowHello2
    /// </summary>
    /// <returns></returns>
    string ShowHello2();
}

/// <summary>
/// </summary>
public interface IInjectServiceTest
{
    /// <summary>
    /// ShowHello
    /// </summary>
    /// <returns></returns>
    string ShowHello();
}