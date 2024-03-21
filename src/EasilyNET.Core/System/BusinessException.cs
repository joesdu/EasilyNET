using System.Net;

namespace EasilyNET.Core.System;

/// <summary>
/// 业务异常,用于处理业务中的异常信息
/// </summary>
/// <param name="code">HTTP请求状态码</param>
/// <param name="message">消息</param>
public class BusinessException(HttpStatusCode code, string message) : Exception(message)
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public HttpStatusCode Code { get; private set; } = code;

    /// <summary>
    /// 消息
    /// </summary>
    public new string Message { get; private set; } = message;
}