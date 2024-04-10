using EasilyNET.Core.Enums;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 测试参数隐藏
/// </summary>
[Route("api/[controller]/[action]"), ApiController]
public class PramsIgnoreController : ControllerBase
{
    /// <summary>
    /// 测试直接忽略参数
    /// </summary>
    /// <param name="test"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    [HttpGet("{a:int}")]
    public void IgnoreParams([SwaggerIgnore] string test = "test", int a = 1, [SwaggerIgnore] int b = 2)
    {
        Console.WriteLine(test);
        Console.WriteLine(a);
        Console.WriteLine(b);
    }

    /// <summary>
    /// 测试直接忽略参数
    /// </summary>
    /// <param name="ic"></param>
    [HttpPost]
    public void IgnoreParams(IgnoreClass ic)
    {
        Console.WriteLine(ic.Test);
        Console.WriteLine(ic.A);
        Console.WriteLine(ic.B);
        Console.WriteLine(ic.Gender);
    }
}

/// <summary>
/// 测试忽略类中的字段
/// </summary>
public class IgnoreClass
{
    /// <summary>
    /// TEST
    /// </summary>
    [SwaggerIgnore]
    public string Test { get; set; } = "test";

    /// <summary>
    /// A
    /// </summary>
    public int A { get; set; }

    /// <summary>
    /// B
    /// </summary>
    [JsonIgnore]
    public int B { get; set; } = 2;

    /// <summary>
    /// 性别
    /// </summary>
    public EGender Gender { get; set; } = EGender.男;
}