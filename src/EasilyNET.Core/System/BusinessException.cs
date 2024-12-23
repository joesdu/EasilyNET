using System.Net;

namespace EasilyNET.Core.System;

/// <summary>
///     <para xml:lang="en">Business exception, used to handle exceptions in business logic</para>
///     <para xml:lang="zh">业务异常，用于处理业务中的异常信息</para>
/// </summary>
/// <param name="code">
///     <para xml:lang="en">HTTP request status code</para>
///     <para xml:lang="zh">HTTP 请求状态码</para>
/// </param>
/// <param name="message">
///     <para xml:lang="en">The exception message</para>
///     <para xml:lang="zh">消息</para>
/// </param>
public class BusinessException(HttpStatusCode code, string message) : Exception(message)
{
    /// <summary>
    ///     <para xml:lang="en">HTTP status code</para>
    ///     <para xml:lang="zh">HTTP 状态码</para>
    /// </summary>
    public HttpStatusCode Code { get; private set; } = code;

    /// <summary>
    ///     <para xml:lang="en">The exception message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </summary>
    public new string Message { get; private set; } = message;
}