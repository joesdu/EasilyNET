#### EasilyNET.AutoDependencyInjection

- 新增 WPF 项目支持,理论上也支持 WinForm 项目,但是没有测试,使用时请注意.(仅限于 .NET 的项目,不支持 .NET Framework)

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

再在 WPF 项目中,使用依赖注入,需要在 AppServiceModules.cs 中添加如下代码: 该类的使用方法和 Web 项目中的 AppWebModule.cs 一样.

```csharp
[DependsOn(typeof(DependencyAppModule))]
internal sealed class AppServiceModules : AppModule { }
```

在 WPF 项目中,使用依赖注入,需要在 MainWindow.xaml.cs 中继承接口或者添加 DependencyInjection 特性如下代码:

```csharp
// 这里特性和接口二选一,推荐使用特性
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public partial class MainWindow : Window, IXXXXDependency

```

##### 注意事项

- 接口的实现类,需要显示的继承 IScopedDependency, ISingletonDependency, ITransientDependency 接口,这些接口中新增 AddSelf 属性,用于标识是否将自己也注册为服务.行为保持和 DependencyInjection 特性中的 AddSelf 属性一致.
- 需要注意的是,在 WPF 项目中,请将 AddSelf 属性设置为 true,否则会出现服务无法找到的问题,因为默认会注册实现类的父类,导致使用 ```host.Services.GetRequiredService<MainWindow>()``` 的方式无法找到服务.WinForm 项目中,没有测试,但是理论上也是一样的.
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

**使用接口注入时需要注意** 
- 由于无法通过接口的方式来约束类中的静态成员,所以我们这里需要做一个约定.在类中写入如下代码来实现和特性相同的功能.(所以更推荐使用特性的方式注入)
- 若是不声明这两个属性,可能会导致注入了其实现的类或接口,影响获取服务的结果.如在 WPF 中注册 MainWindow.cs,会注册其实现的接口类型.导致无法获取到正确的实现.
- 这里采用较长的名字,避免和类中别的成员出现名称冲突.
- DependencyInjectionSelf 对应 DependencyInjection 特性中的 AddSelf
- DependencyInjectionSelfOnly 对应 DependencyInjection 特性中的 SelfOnly
```csharp
/// <summary>
/// 是否添加自身
/// </summary>
// ReSharper disable once UnusedMember.Global
public static bool? DependencyInjectionSelf => true;

/// <summary>
/// 仅注册自身,而不注其父类或者接口
/// </summary>
// ReSharper disable once UnusedMember.Global
public static bool? DependencyInjectionSelfOnly => true;
```

##### 如何使用

- 使用 Nuget 包管理工具添加依赖包 EasilyNET.AutoDependencyInjection
- a.使用特性注入服务

```csharp
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true, SelfOnly = true)]
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
