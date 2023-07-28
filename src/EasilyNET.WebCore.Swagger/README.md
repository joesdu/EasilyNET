### EasilyNET.WebCore.Swagger

将 Swagger 的扩展独立出来,避免 WebCore 的过度依赖.

### 如何使用?

- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

```csharp
// 添加 Swagger 服务
private const string name = $"{title}-{version}";

private const string version = "v1";
private const string title = "WebApi.Test";

builder.Services.AddSwaggerGen(c =>
{
    // 配置默认的文档信息
    c.SwaggerDoc(name, new()
    {
        Title = title,
        Version = version,
        Description = "Console.WriteLine(\"🐂🍺\")"
    });
    // 这里使用EasilyNET提供的扩展配置.
    c.EasilySwaggerGenOptions(name);
    // 配置认证方式
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

...

// 注册 Swagger 中间件
app.UseSwagger().UseSwaggerUI(c =>
{
    // 配置默认文档
    c.SwaggerEndpoint($"/swagger/{name}/swagger.json", $"{title} {version}");
    // 使用EasilyNET提供的扩展配置
    c.EasilySwaggerUIOptions();
});

```
