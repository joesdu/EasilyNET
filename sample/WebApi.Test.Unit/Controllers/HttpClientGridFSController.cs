using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 测试HttpClient上传文件的问题复现
/// </summary>
[Route("api/[controller]"), ApiController, ApiGroup("GridFSTest", "v1", "HttpClientGridFSController")]
public class HttpClientGridFSController : ControllerBase
{
    /// <summary>
    /// 测试使用HttpClient上传文件.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<string> PostFile()
    {
        var httpClient = new HttpClient();
        var videoFIleId = string.Empty;
        using var videoContent = new MultipartFormDataContent();
        // 替换成你自己的测试文件
        var file = await System.IO.File.ReadAllBytesAsync(@"F:\Desktop\aspnetcore-developer-roadmap.zh-Hans.xmind");
        var byteArrayContent = new ByteArrayContent(file);
        videoContent.Add(byteArrayContent, "File", "aspnetcore-developer-roadmap.zh-Hans.xmind");
        // 这里推荐指定下文件ContentType.
        //byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        var result = await httpClient.PostAsync("http://localhost:5046/api/GridFS/UploadSingle", videoContent);
        if (result.IsSuccessStatusCode)
        {
            videoFIleId = await result.Content.ReadAsStringAsync();
        }
        return videoFIleId;
    }
}
