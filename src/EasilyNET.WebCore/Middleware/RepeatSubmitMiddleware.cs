using EasilyNET.WebCore.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EasilyNET.WebCore.Middleware;

/// <summary>
/// 防抖中间件
/// </summary>
/// <param name="next"></param>
/// <param name="cache"></param>
internal sealed class RepeatSubmitMiddleware(RequestDelegate next, IDistributedCache cache)
{
    /// <summary>
    /// Md5前面加上这个
    /// </summary>
    private const string slat = "oN?W)Yf%!bD&G&D4TZqso>%zTL9pNJBf";

    /// <summary>
    /// 缓存Key
    /// </summary>
    private const string CacheKey = "repeat:submit:";

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Method.ToLower().Equals("option"))
        {
            var endpoint = context.GetEndpoint();
            if (endpoint is not null)
            {
                var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                var ctrlAttr = actionDescriptor?.ControllerTypeInfo.GetCustomAttribute<RepeatSubmitAttribute>();
                var actionAttr = endpoint.Metadata.GetMetadata<RepeatSubmitAttribute>();
                var myAttribute = actionAttr ?? ctrlAttr;
                if (myAttribute is not null)
                {
                    var hash = $"""
                                {context.Request.Host},
                                {context.Request.Method},
                                {context.Request.Path},
                                {context.Request.Scheme},
                                {context.Request.HttpContext.Connection.RemoteIpAddress},
                                {context.Request.ContentLength},
                                {context.Request.Cookies},
                                {context.Request.ContentType},
                                {slat},
                                {context.Request.GetDisplayUrl()}
                                """.To32MD5();
                    var key = $"{CacheKey}:{hash}";
                    // 判断缓存中有没有（最好用redis）
                    var value = await cache.GetStringAsync(key);
                    if (value is null)
                    {
                        var cacheOptions = new DistributedCacheEntryOptions();
                        cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(myAttribute.Interval));
                        await cache.SetStringAsync(key, hash, cacheOptions);
                    }
                    else
                    {
                        throw new("不允许重复提交,请稍候再试");
                    }
                }
            }
        }
        await next(context);
    }
}

static file class PrivateFileClass
{
    /// <summary>
    /// 获取32位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static string To32MD5(this string value)
    {
        var data = MD5.HashData(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder();
        // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串 
        foreach (var t in data) _ = builder.Append(t.ToString("X2"));
        return builder.ToString();
    }
}