using EasilyNET.Images.QrCode;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 二维码
/// </summary>
[Route("api/[controller]"), ApiController, ApiGroup("QrCode", "v1")]
public class QrCodeController : ControllerBase
{
    /// <summary>
    /// QrCode
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    [HttpGet("Encode/{str}")]
    public string Encode(string str) => QrCode.Encode(str);

    /// <summary>
    /// 从Base64中解析字符串
    /// </summary>
    /// <param name="base64"></param>
    /// <returns></returns>
    [HttpPost("Decode")]
    public string Decode([FromBody] string base64) => QrCode.Decode(base64);
}
