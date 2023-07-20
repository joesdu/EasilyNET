using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EasilyNET.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

// ReSharper disable UnusedType.Global
namespace EasilyNET.WebCore.Filters;

/// <summary>
/// 重复请求过滤器
/// </summary>
/// <example>
///  <code>
/// <![CDATA[
///  [TypeFilter(typeof(RepeatSubmitFilter))]
///  ]]>
///  </code>
/// </example>
// ReSharper disable once ClassNeverInstantiated.Global
public class RepeatSubmitFilter : ActionFilterAttribute
{
    
    private const string tokenName = "Authorization";

    /// <summary>
    /// Md5前面加上这个
    /// </summary>
    private const string Md5Secret = "YnHipYQ36RiAwJy";
    
    /// <summary>
    /// 缓存Key
    /// </summary>
    private const string CacheKey = "repeat:submit:";
    

    
    private readonly IDistributedCache _cache;
    
    
   /// <summary>
   /// 构造函数
   /// </summary>
   /// <param name="cache">缓存</param>
   /// <param name="interval">间隔时间默认</param>
    public RepeatSubmitFilter(IDistributedCache cache,int interval = 60000)
    {
        _cache = cache;
        Interval = interval;
    }


 
    /// <summary>
    /// 间隔时间默认（60000）ms
    /// </summary>
    private int Interval { get;  set; }
    
    /// <summary>
    /// 方法开始执行
    /// </summary>
    /// <param name="context">上下文</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Int64 interval = 0;
        if (Interval > 0)
        {
            interval = (Int64) TimeSpan.FromMilliseconds(Interval).TotalMilliseconds;
        }

        //不应该抛出异常
        if (interval < 1000)
        {
            throw new Exception("重复提交间隔时间不能小于'1'秒");
        }
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        //没有共用方法吗？？？？、
        var jsonParams = JsonSerializer.Serialize(context.ActionArguments, options);

        //请求地址
        string url = context.HttpContext.Request.Path.Value?.ToLower()!;

        //暂时不使用
        // 唯一值（没有消息头则使用请求地址）
        // string submitKey = context.HttpContext.Request.Headers[tokenName];
        //
        // if (!string.IsNullOrWhiteSpace(submitKey))
        // {
        //     string pattern = "^Bearer (.*?)$";
        //     Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
        //     Match match = regex.Match(submitKey1 );
        //     if (match.Success)
        //     {
        //         
        //         submitKey  = match.Groups[1]?.Value?.Trim();
        //     }
        // }
        string submitKey = $"{Md5Secret}{jsonParams}".To32MD5().ToLower();
        // 唯一标识（指定key + url + 消息头）
        string cacheRepeatKey = $"{CacheKey}{url}:{submitKey}";

        //判断缓存中有没有（最好用redis）
        var value = _cache.GetString(cacheRepeatKey);

        if (value == null)
        {
            DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions();
            cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(interval));
            _cache.SetString(cacheRepeatKey, submitKey, cacheOptions);
        }
        else
        {

            context.Result = new ObjectResult(new ResultObject {StatusCode = HttpStatusCode.OK, Msg = "不允许重复提交，请稍候再试", Data = default});
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// 执行后
    /// </summary>
    /// <param name="context">上下文</param>
    public override void OnResultExecuted(ResultExecutedContext context)
    {
        //成功则不删除数据 保证在有效时间内无法重复提交
        if (context.Result is ObjectResult {Value: ResultObject {StatusCode: HttpStatusCode.OK}})
           
        {
            return;
        }

        // var keyCache = KeyCache.Value;
        // _cache.Remove(keyCache!);
        // KeyCache.Value = null!;
        base.OnResultExecuted(context);
    }


}