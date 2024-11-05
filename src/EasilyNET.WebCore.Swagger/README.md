### EasilyNET.WebCore.Swagger

将 Swagger 的扩展独立出来,避免 WebCore 的过度依赖.

- 新增 Swagger 页面参数忽略.比如某些默认参数不需要调用者传入,并且也不希望他看见
- 接口隐藏,或者控制器隐藏
-

添加默认值显示 [代码示例](https://github.com/EasilyNET/EasilyNET/tree/main/Test/WebApi.Test.Unit/Controllers/MongoTestController.cs)
- 在 Swagger 文档中给需要 Authorize 的接口添加 🔒

### 最新变化

- SwaggerIgnoreAttribute由于官方已经提供了同名特性,所以这里删除相关代码.

### 可用特性

- ApiGroupAttribute 对控制器进行分组.便于将特有的功能分到一个组方便管理.
- HiddenApiAttribute 隐藏控制器或者单个接口.
- SwaggerIgnoreAttribute
  忽略接口参数或者传入实体的属性 [代码示例](https://github.com/EasilyNET/EasilyNET/tree/main/Test/WebApi.Test.Unit/Controllers/PramsIgnoreController.cs)

### 如何使用?

- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [完整代码示例](https://github.com/EasilyNET/EasilyNET/tree/main/Test/WebApi.Test.Unit/ServiceModules/SwaggerModule.cs)

```csharp
// 添加 Swagger 服务

builder.Services.AddSwaggerGen(c =>
{
    // 这里使用EasilyNET提供的扩展配置.
    c.EasilySwaggerGenOptions();
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
app.UseEasilySwaggerUI();

```
