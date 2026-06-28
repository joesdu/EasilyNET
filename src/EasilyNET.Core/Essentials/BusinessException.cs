using System.Net;

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">Business exception, used to handle exceptions in business logic</para>
///     <para xml:lang="zh">业务异常，用于处理业务中的异常信息</para>
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of <see cref="BusinessException" />.</para>
    ///     <para xml:lang="zh">初始化 <see cref="BusinessException" /> 的新实例。</para>
    /// </summary>
    /// <param name="code">
    ///     <para xml:lang="en">HTTP request status code</para>
    ///     <para xml:lang="zh">HTTP 请求状态码</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The exception message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
    public BusinessException(HttpStatusCode code, string message) : base(message) => Code = code;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of <see cref="BusinessException" /> with an inner exception.</para>
    ///     <para xml:lang="zh">使用内部异常初始化 <see cref="BusinessException" /> 的新实例。</para>
    /// </summary>
    /// <param name="code">
    ///     <para xml:lang="en">HTTP request status code</para>
    ///     <para xml:lang="zh">HTTP 请求状态码</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The exception message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
    /// <param name="innerException">
    ///     <para xml:lang="en">The exception that caused the current exception</para>
    ///     <para xml:lang="zh">导致当前异常的内部异常</para>
    /// </param>
    public BusinessException(HttpStatusCode code, string message, Exception? innerException) : base(message, innerException) => Code = code;

    /// <summary>
    ///     <para xml:lang="en">HTTP status code</para>
    ///     <para xml:lang="zh">HTTP 状态码</para>
    /// </summary>
    // Note: Message is intentionally inherited from Exception (no shadowing) so it is preserved when the
    // exception is caught as a base Exception or serialized/logged.
    public HttpStatusCode Code { get; }
}