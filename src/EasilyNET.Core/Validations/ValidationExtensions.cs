//using System.Net;
//using FluentValidation;

//namespace EasilyNET.Core.Validations; 

///// <summary>
///// 验证扩展
///// </summary>
//public static class ValidationExtensions
//{
//    /// <summary>
//    /// 验证错误标准
//    /// </summary>
//    /// <param name="ex"></param>
//    /// <returns></returns>
//    public static ProblemDetails ToProblemDetails(this ValidationException ex)
//    {
//        var error = new ProblemDetails()
//        {
//            //Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
//            // 上边的是英文的,换成国内中文的说明
//            Url = "https://tool.oschina.net/commons?type=5",
//            Title = "发生了一个或多个验证错误",
//            StatusCode = (int)HttpStatusCode.BadRequest
//        };
//        //返回的错误格式自行修改,不建议Errors 用字典,页面显示麻烦
//        foreach (var validationFailure in ex.Errors)
//        {
//            var errors = error.Errors;
//            if (errors.ContainsKey(validationFailure.PropertyName))
//            {
//                errors[validationFailure.PropertyName] = errors[validationFailure.PropertyName].Concat(new[] { validationFailure.ErrorMessage }).ToArray();
//                continue;
//            }
//            error.Errors.Add(validationFailure.PropertyName, new[] { validationFailure.ErrorMessage });
//        }
//        return error;
//    }
//}

/////  可以参考微软Microsoft.AspNetCore.Http.Extensions.ProblemDetails
///// <summary>
///// 标准类
///// </summary>
//public class ProblemDetails
//{
//    /// <summary>
//    /// 错误代码类型查阅地址
//    /// </summary>
//    public string Url { get; set; } = string.Empty;
//    /// <summary>
//    /// 标题
//    /// </summary>
//    public string Title { get; set; } = string.Empty;
//    /// <summary>
//    /// 状态码
//    /// </summary>
//    public int StatusCode { get; set; }
//    /// <summary>
//    /// 错误
//    /// </summary>
//    public Dictionary<string, string[]> Errors { get; set; } = new();
//}

