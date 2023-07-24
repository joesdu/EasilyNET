using EasilyNET.Security;
using EasilyNET.WebCore.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using System.Reflection;

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