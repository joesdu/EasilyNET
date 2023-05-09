﻿using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 一些接口上的功能测试
/// </summary>
[Route("api/[controller]/[action]"), ApiController]
public class ValuesController : ControllerBase
{
    /// <summary>
    /// Error
    /// </summary>
    [HttpGet]
    public void GetError() => throw new("测试异常");
}