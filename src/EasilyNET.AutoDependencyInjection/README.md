#### EasilyNET.AutoDependencyInjection

- 新增KeyedService支持,可在 DependencyInjectionAttribute 中看到对应的 ServiceKey 属性,用于标识服务的 Key 值.
- 新增 WPF, WinForms, WinUI3 项目支持.(仅限于 .NET 的项目,不支持 .NET Framework)
- 经测试是支持 WinUI 3 类型的项目的,但是需要注意的是,WinUI 3 项目的启动方式和 WPF 项目不一样,需要自行调整.
- [WPF例子](https://github.com/joesdu/WpfAutoDISample) 已同步到最新代码.
- [WinForms例子](https://github.com/joesdu/WinFormAutoDISample) 已同步到最新代码.
- [WinUI3例子](https://github.com/joesdu/WinUIAutoDISample) 暂时没同步到最新版本,可以自己更新一下,目前暂时没有WinUI环境所以没更新.

##### 中断性变更

- 移除使用 IScopedDependency, ISingletonDependency, ITransientDependency 接口的方式,仅使用特性注入的方式.保持设计简洁性.

##### 变化

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

同时还需要调整 .csproj 文件,添加如下代码:

```xml
<ItemGroup>
	<ApplicationDefinition Remove="App.xaml" />
	<Page Include="App.xaml" />
</ItemGroup>
```

再在 WPF 项目中,使用依赖注入,需要在 AppServiceModules.cs 中添加如下代码: 该类的使用方法和 Web 项目中的 AppWebModule.cs
一样.

```csharp
[DependsOn(typeof(DependencyAppModule))]
internal sealed class AppServiceModules : AppModule { }
```

在 WPF 项目中,使用依赖注入,需要在 MainWindow.xaml.cs 中继承接口或者添加 DependencyInjection 特性如下代码:

```csharp
// 使用特性配置注入信息
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true, SelfOnly = true)]
public partial class MainWindow : Window

```

##### 注意事项

- 需要注意的是,在 WPF 项目中,请将 AddSelf 属性设置为 true,否则会出现服务无法找到的问题,因为默认会注册实现类的父类,导致使用
  ```host.Services.GetRequiredService<MainWindow>()``` 的方式无法找到服务.WinForm 项目中,没有测试,但是理论上也是一样的.
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
        var app = context.GetApplicationBuilder() as IApplicationBuilder;
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
        var app = context.GetApplicationBuilder() as IApplicationBuilder;
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
