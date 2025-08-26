#### EasilyNET.AutoDependencyInjection

- 新增 KeyedService 支持,可在 `DependencyInjectionAttribute` 中看到对应的 `ServiceKey` 属性,用于标识服务的 Key 值.
- 新增 WPF, WinForms, WinUI3 项目支持.(仅限于 .NET 的项目,不支持 .NET Framework)
- 经测试是支持 WinUI 3 类型的项目的,但是需要注意的是,WinUI 3 项目的启动方式和 WPF 项目不一样,需要自行调整.
- [WPF 例子](https://github.com/joesdu/WpfAutoDISample) 已同步到最新代码.
- [WinForms 例子](https://github.com/joesdu/WinFormAutoDISample) 已同步到最新代码.
- [WinUI3 例子](https://github.com/joesdu/WinUIAutoDISample) 暂时没同步到最新版本,可以自己更新一下,目前暂时没有 WinUI 环境所以没更新.

##### 新增特性

- 新增 `GetEnable` 函数,该函数未重写的情况下默认返回 `true`,可通过重写该函数,实现从配置文件中读取是否启用服务.<br/>比如
  `SwaggerUI` 服务,我们可能仅希望在预发布模式中启用,为前端工程师提供 `SwaggerUI`,当部署到生产环境后,通过直接修改配置文件,即可关闭
  `SwaggerUI`.
- 移除掉原有 `Enable` 属性,改为 `GetEnable` 函数,用于更灵活的配置服务.

#### 中断性变更

- 调整 `ConfigureServices` 和 `ApplicationInitialization` 为异步方式,便于在某些时候初始化服务的时候使用异步版本.

##### Resolver 使用说明

- 通过 `IResolver` 或 `IServiceProvider.CreateResolver()` 可获得更灵活的服务解析能力,支持 KeyedService、带参数构造等高级场景.
- 典型用法：

```csharp
var resolver = provider.CreateResolver(); 
var service = resolver.ResolveKeyed<IMyService>("MyKey", new NamedParameter("param1", value1));
```

- 支持通过参数名和实例动态注入构造参数,适合无默认构造函数或需要运行时参数的服务.

---

##### 注意事项

- Resolver 支持 KeyedService 及参数注入,适合复杂依赖场景.

---

##### WPF 中的使用

由于新增了 WPF 项目支持,所以在使用时需要注意以下几点:
WPF 项目中,使用依赖注入,需要在 App.xaml.cs 中添加如下代码:

```csharp
[STAThread]
public static void Main(string[] args)
{
    using var host = CreateHostBuilder(args).Build();
    host.InitializeApplication();
    host.Start();
    var app = new App();
    app.InitializeComponent();
    app.MainWindow = host.Services.GetRequiredService<MainWindow>();
    app.MainWindow.Visibility = Visibility.Visible;
    app.Run();
}

private static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
               .ConfigureServices(sc => { sc.AddApplicationModules<AppServiceModules>(); });
}
```

同时还需要调整 `.csproj` 文件,添加如下代码:

```xml
<ItemGroup>
	<ApplicationDefinition Remove="App.xaml" />
	<Page Include="App.xaml" />
</ItemGroup>
```

再在 WPF 项目中,使用依赖注入,需要在 `AppServiceModules.cs` 中添加如下代码: 该类的使用方法和 Web 项目中的
`AppWebModule.cs`
一样.

```csharp
[DependsOn(typeof(DependencyAppModule))]
internal sealed class AppServiceModules : AppModule { }
```

在 WPF 项目中,使用依赖注入,需要在 `MainWindow.xaml.cs` 中继承接口或者添加 `DependencyInjection` 特性如下代码:

```csharp
// 使用特性配置注入信息
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true, SelfOnly = true)]
public partial class MainWindow : Window

```

##### 注意事项

- 需要注意的是,在 WPF 项目中,请将 AddSelf 属性设置为 true,否则会出现服务无法找到的问题,因为默认会注册实现类的父类,导致使用
  `host.Services.GetRequiredService<MainWindow>()` 的方式无法找到服务.WinForm 项目中,没有测试,但是理论上也是一样的.
- 由于新增 WPF 项目支持,所以调整了 IApplicationBuilder 为 IHost,因此 WEB 项目中的使用方式有细微的变化.

```csharp
// 之前的使用方式
IApplicationBuilder app = context.GetApplicationBuilder();
// 现在的使用方式
IApplicationBuilder app = context.GetApplicationHost() as IApplicationBuilder;
// 或者如下方式,根据实际情况选择
WebApplication app = context.GetApplicationHost() as WebApplication;
// 在 WPF 或者 WinForm 项目中,使用如下方式
IHost app = context.GetApplicationHost();
```

##### 如何使用

- 使用 Nuget 包管理工具添加依赖包 EasilyNET.AutoDependencyInjection
- 使用特性注入服务

```csharp
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true, SelfOnly = true)]
public class XXXService : IXXXService
{
    // TODO: do something
    Console.WriteLine("使用特性注入服务");
    ...
}
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
    // 新增函数,用于可从配置文件读取是否启用服务
    public override bool GetEnable(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        return config.GetSection("ServicesEnable").GetValue<bool>("Cors");
    }

    /// <summary>
    /// 注册和配置服务
    /// </summary>
    /// <param name="context"></param>
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        var allow = config["AllowedHosts"] ?? "*";
        _ = context.Services.AddCors(c => c.AddPolicy("AllowedHosts", s => s.WithOrigins(allow.Split(",")).AllowAnyMethod().AllowAnyHeader()));
        await Task.CompletedTask;
    }
    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder() as IApplicationBuilder;
        _ = app.UseCors("AllowedHosts");
        await Task.CompletedTask;
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
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        _ = context.Services.AddHttpContextAccessor();
        await base.ConfigureServices(context);
    }
    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder() as IApplicationBuilder;
        _ = app.UseAuthorization();
        // 这里可添加自己的中间件
        await base.ApplicationInitialization(context);
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
