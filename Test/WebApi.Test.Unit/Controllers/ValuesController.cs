using EasilyNET.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 一些接口上的功能测试
/// </summary>
[Route("api/[controller]/[action]"), ApiController]
public class ValuesController : ControllerBase
{


    public ValuesController()
    {


    }

    /// <summary>
    /// Error
    /// </summary>
    [HttpGet]
    public void GetError() => throw new("测试异常");

    /// <summary>
    /// 测试枚举类型
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public EGender GetEnumTest() => EGender.男;
}