#### EasilyNET.AutoDependencyInjection

自动注入模块,参考 ABP 的代码实现.

##### 如何使用

- 使用 Nuget 包管理工具添加依赖包 EasilyNET.AutoDependencyInjection
- 等待下载完成和同意开源协议后,即可使用本库.
- a.使用特性注入服务

```csharp
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class XXXService : IXXXService
{
    // TODO: do something
    Console.WriteLine("使用特性注入服务");
    ...
}
```

- b.使用接口的方式,继承 IScopedDependency, ISingletonDependency, ITransientDependency

```csharp
/// <summary>
/// 测试模块
/// </summary>
public class MyTestModule : ITest, IScopedDependency
{
    /// <summary>
    /// Show
    /// </summary>
    public void Show()
    {
        Console.WriteLine("Test");
    }
}

/// <summary>
/// 测试
/// </summary>
//[IgnoreDependency]
public interface ITest
{
    /// <summary>
    /// Show函数
    /// </summary>
    void Show();
}

// 在其他地方获取服务,并执行方法
var test = context.Services.GetService<MyTestModule>();
test?.Show();
...
```

- 3.继承 AppModule 类,然后显示加入到 AppWebModule 配置中
- Step1.创建 CorsModule.cs

```csharp
// 这里以跨域服务注册为例
/// <summary>
/// 配置跨域服务及中间件
/// </summary>
public class CorsModule : AppModule
{
    /// <summary>
    /// 注册和配置服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        var allow = config["AllowedHosts"] ?? "*";
        _ = context.Services.AddCors(c => c.AddPolicy("AllowedHosts", s => s.WithOrigins(allow.Split(",")).AllowAnyMethod().AllowAnyHeader()));
    }
    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder();
        _ = app.UseCors("AllowedHosts");
    }
}
```

- Step2.创建 AppWebModule.cs

```csharp
/**
 * 要实现自动注入,一定要在这个地方添加
 */
[DependsOn(
    typeof(DependencyAppModule),
    typeof(CorsModule)
)]
public class AppWebModule : AppModule
{
    /// <summary>
    /// 注册和配置服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        base.ConfigureServices(context);
        _ = context.Services.AddHttpContextAccessor();
    }
    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override void ApplicationInitialization(ApplicationContext context)
    {
        base.ApplicationInitialization(context);
        var app = context.GetApplicationBuilder();
        _ = app.UseAuthorization();
        // 这里可添加自己的中间件
    }
}
```

- Step3.最后再 Program.cs 中添加如下内容.

```csharp
// Add services to the container.
// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) _ = app.UseDeveloperExceptionPage();

// 添加自动化注入的一些中间件.
app.InitializeApplication();

app.MapControllers();

app.Run();
```
