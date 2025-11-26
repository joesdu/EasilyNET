using System.Net;
using EasilyNET.Core.Enums;
using EasilyNET.Core.Essentials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 一些接口上的功能测试
/// </summary>
[Route("api/[controller]/")]
[ApiController]
public class ValuesController : ControllerBase
{
    /// <summary>
    /// Error
    /// </summary>
    [HttpGet("Error")]
    [Authorize]
    public void GetError() => throw new BusinessException(HttpStatusCode.Forbidden, "403异常");

    /// <summary>
    /// Error
    /// </summary>
    [HttpGet("Error2")]
    public void GetError2() => throw new("500异常,来自其他非BusinessException");

    /// <summary>
    /// 空
    /// </summary>
    [HttpGet("Null")]
    public void GetNull()
    {
        Console.WriteLine(nameof(GetNull));
    }

    /// <summary>
    /// 测试枚举类型
    /// </summary>
    /// <returns></returns>
    [HttpGet("Enum")]
    public EGender GetEnumTest() => EGender.男;

    /// <summary>
    /// 测试枚举类型
    /// </summary>
    /// <returns></returns>
    [HttpGet("Object")]
    public dynamic GetObject() =>
        new
        {
            Name = "张三",
            Age = 21,
            Gender = EGender.女
        };
}