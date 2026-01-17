#### EasilyNET.AutoDependencyInjection

一个功能强大的自动依赖注入模块系统，提供模块化的服务配置和中间件管理能力。

---

### **核心特性**

#### 1. **模块化架构 (AppModule)**

- 基于 `AppModule` 的模块系统，支持依赖关系声明 (`DependsOn`)
- 模块加载顺序自动解析，确保依赖模块优先初始化
- 支持通过 `GetEnable` 方法动态控制模块启用/禁用（可从配置文件读取）

#### 2. **KeyedService 支持**

- 完整支持 .NET 的 KeyedService 功能
- 可在 `DependencyInjectionAttribute` 中使用 `ServiceKey` 属性标识服务键值
- 支持通过 `ResolveKeyed<T>(key)` 解析键控服务

#### 3. **高级服务解析器 (IResolver)**

- 提供类似 Autofac 的动态解析能力，同时基于 `Microsoft.Extensions.DependencyInjection`
- 支持构造函数参数覆盖 (NamedParameter, TypedParameter, ResolvedParameter)
- 支持可选解析、批量解析、命名解析、键控解析
- 支持创建独立作用域 (`BeginScope`)

#### 4. **多平台支持**

- **Web 应用**: ASP.NET Core (WebApplication, IApplicationBuilder)
- **桌面应用**: WPF, WinForms, WinUI3 (.NET 项目，不支持 .NET Framework)
- 统一的 API 接口，便于跨平台项目复用模块

#### 5. **异步优先设计**

- `ConfigureServices` 和 `ApplicationInitialization` 均为异步方法
- 支持 `InitializeApplicationAsync` 用于异步初始化
- 支持 `CancellationToken` 取消操作

---

### **示例项目**

| 平台     | 示例项目                                                       | 状态      |
| -------- | -------------------------------------------------------------- | --------- |
| WPF      | [WPF 示例](https://github.com/joesdu/WpfAutoDISample)          | ✅ 最新   |
| WinForms | [WinForms 示例](https://github.com/joesdu/WinFormAutoDISample) | ✅ 最新   |
| WinUI3   | [WinUI3 示例](https://github.com/joesdu/WinUIAutoDISample)     | ⚠️ 待更新 |

---

### **Resolver 高级解析器**

`IResolver` 提供比原生 `IServiceProvider` 更强大的服务解析能力。

#### 核心方法

| 方法                     | 说明                         |
| ------------------------ | ---------------------------- |
| `Resolve<T>()`           | 解析服务，失败抛异常         |
| `TryResolve<T>(out var)` | 尝试解析服务，失败返回 false |
| `ResolveOptional<T>()`   | 解析可选服务，失败返回 null  |
| `ResolveAll<T>()`        | 解析所有已注册的 T 服务      |
| `ResolveKeyed<T>(key)`   | 解析键控服务（KeyedService） |
| `ResolveNamed<T>(name)`  | 解析命名服务                 |
| `BeginScope()`           | 创建子作用域                 |

#### 构造函数参数注入

支持三种参数类型：

1. **NamedParameter**: 按参数名匹配
2. **TypedParameter**: 按参数类型匹配
3. **ResolvedParameter**: 自定义匹配逻辑和值提供

#### 使用示例

```csharp
// 1. 基本解析
var resolver = provider.CreateResolver();
var service = resolver.Resolve<IMyService>();

// 2. 带参数覆盖的解析
var service = resolver.Resolve<IMyService>(
    new NamedParameter("connectionString", "Server=localhost"),
    new TypedParameter(typeof(ILogger), logger)
);

// 3. 键控服务解析
var keyedService = resolver.ResolveKeyed<ICache>("redis",
    new NamedParameter("endpoint", "127.0.0.1:6379")
);

// 4. 批量解析
var allHandlers = resolver.ResolveAll<IEventHandler>();

// 5. 可选解析
var optional = resolver.ResolveOptional<IOptionalService>();

// 6. 作用域解析
using var scopedResolver = resolver.BeginScope();
var scopedService = scopedResolver.Resolve<IScopedService>();
```

#### IServiceProvider 扩展方法

也可以直接在 `IServiceProvider` 上使用这些能力：

```csharp
// 创建 Resolver（可选择是否创建作用域）
var resolver = provider.CreateResolver(createScope: true);

// 或者直接使用扩展方法
var service = provider.Resolve<IMyService>();
var keyed = provider.ResolveKeyed<ICache>("redis");
var withParams = provider.Resolve<MyService>(
    new NamedParameter("config", configuration)
);
```

#### 性能优化

- 构造函数信息和参数元数据被缓存，避免重复反射
- 优先选择能满足所有参数的构造函数
- 支持 `[FromKeyedServices]` 特性注入键控依赖

---

### **WPF/WinForms 桌面应用集成**

#### WPF 项目配置

**1. 修改 App.xaml.cs**

```csharp
public partial class App : Application
{
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
                   .ConfigureServices(sc =>
                   {
                       sc.AddApplicationModules<AppServiceModules>();
                   });
    }
}
```

**2. 调整 .csproj 文件**

```xml
<ItemGroup>
    <ApplicationDefinition Remove="App.xaml" />
    <Page Include="App.xaml" />
</ItemGroup>
```

**3. 创建模块类 (AppServiceModules.cs)**

```csharp
[DependsOn(typeof(DependencyAppModule))]
internal sealed class AppServiceModules : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        // 注册应用服务
        await base.ConfigureServices(context);
    }
}
```

**4. 注册窗口和服务**

```csharp
// 使用特性注册窗口（注意需要 AddSelf = true）
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true, SelfOnly = true)]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

#### ⚠️ 桌面应用注意事项

1. **AddSelf 必须设置为 true**
   - 默认情况下会注册实现类的父类（如 Window），导致无法通过 `GetRequiredService<MainWindow>()` 获取
   - 设置 `AddSelf = true, SelfOnly = true` 确保注册具体的窗口类型

2. **获取 IHost 的方式不同**

   ```csharp
   // Web 项目
   var app = context.GetApplicationHost() as WebApplication;
   // 或
   var app = context.GetApplicationHost() as IApplicationBuilder;

   // 桌面项目（WPF/WinForms）
   var host = context.GetApplicationHost() as IHost;
   ```

---

### **Web 应用集成 (ASP.NET Core)**

#### 快速开始

**1. 使用特性注入服务**

```csharp
// 标记服务类，自动注入到容器
[DependencyInjection(ServiceLifetime.Scoped)]
public class OrderService : IOrderService
{
    private readonly IRepository _repository;

    public OrderService(IRepository repository)
    {
        _repository = repository;
    }
}

// 支持 KeyedService
[DependencyInjection(ServiceLifetime.Singleton, ServiceKey = "redis")]
public class RedisCache : ICache
{
    // ...
}
```

**2. 创建模块 (AppModule)**

```csharp
// Step 1: 创建功能模块（如 CORS 配置模块）
public class CorsModule : AppModule
{
    // 可从配置文件读取是否启用此模块
    public override bool GetEnable(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        return config.GetSection("ServicesEnable").GetValue<bool>("Cors");
    }

    // 注册服务
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        var allow = config["AllowedHosts"] ?? "*";

        context.Services.AddCors(c =>
            c.AddPolicy("AllowedHosts", s =>
                s.WithOrigins(allow.Split(","))
                 .AllowAnyMethod()
                 .AllowAnyHeader()));

        await Task.CompletedTask;
    }

    // 配置中间件
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseCors("AllowedHosts");

        await Task.CompletedTask;
    }
}
```

**3. 创建根模块**

```csharp
// Step 2: 使用 DependsOn 声明模块依赖关系
[DependsOn(
    typeof(DependencyAppModule),  // 必须依赖，提供自动注入功能
    typeof(CorsModule)             // 自定义模块
)]
public class AppWebModule : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddHttpContextAccessor();
        // 其他服务注册
        await base.ConfigureServices(context);
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseAuthorization();
        // 其他中间件配置
        await base.ApplicationInitialization(context);
    }
}
```

**4. 在 Program.cs 中启用**

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册模块系统
builder.Services.AddApplicationModules<AppWebModule>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 初始化模块（执行所有模块的 ApplicationInitialization）
app.InitializeApplication();

// 或使用异步版本
// await app.InitializeApplicationAsync();

app.MapControllers();
app.Run();
```

---

### **模块化架构最佳实践**

#### 模块依赖顺序

模块的 `DependsOn` 顺序决定了初始化顺序。被依赖的模块会先执行：

```csharp
[DependsOn(
    typeof(DependencyAppModule),    // 第 1 个初始化
    typeof(DatabaseModule),         // 第 2 个初始化
    typeof(CachingModule),          // 第 3 个初始化
    typeof(AuthenticationModule)    // 第 4 个初始化
)]
public class AppWebModule : AppModule  // 最后初始化
{
    // ...
}
```

#### 模块职责划分

建议按功能领域划分模块：

```csharp
// 数据库模块
public class DatabaseModule : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        // 注册 DbContext, Repository 等
    }
}

// 认证模块
public class AuthenticationModule : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        // 注册 JWT, Identity 等
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseAuthentication();
        app?.UseAuthorization();
    }
}

// Swagger 文档模块
public class SwaggerModule : AppModule
{
    public override bool GetEnable(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        return config.GetValue<bool>("Swagger:Enabled");
    }

    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddSwaggerGen();
        await Task.CompletedTask;
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseSwagger();
        app?.UseSwaggerUI();
        await Task.CompletedTask;
    }
}
```

#### 配置驱动的模块启用

在 `appsettings.json` 中配置模块开关：

```json
{
  "ServicesEnable": {
    "Cors": true,
    "Swagger": true,
    "HealthChecks": false
  }
}
```

在模块中读取配置：

```csharp
public override bool GetEnable(ConfigureServicesContext context)
{
    var config = context.ServiceProvider.GetConfiguration();
    return config.GetSection("ServicesEnable").GetValue<bool>("Swagger");
}
```

---

### **DependencyInjection 特性说明**

#### 特性属性

| 属性         | 类型            | 说明                                       | 默认值 |
| ------------ | --------------- | ------------------------------------------ | ------ |
| `Lifetime`   | ServiceLifetime | 服务生命周期（Singleton/Scoped/Transient） | Scoped |
| `ServiceKey` | object?         | 键控服务的键值（KeyedService）             | null   |
| `AddSelf`    | bool            | 是否注册实现类自身                         | false  |
| `SelfOnly`   | bool            | 是否仅注册实现类（不注册接口）             | false  |

#### 使用示例

```csharp
// 基础用法：注册接口
[DependencyInjection(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    // 会注册 IUserService -> UserService
}

// 键控服务
[DependencyInjection(ServiceLifetime.Singleton, ServiceKey = "primary")]
public class PrimaryDatabase : IDatabase
{
    // 会注册 Keyed Service: "primary" -> PrimaryDatabase
}

// 同时注册接口和实现类
[DependencyInjection(ServiceLifetime.Scoped, AddSelf = true)]
public class ProductService : IProductService
{
    // 会注册两个：
    // 1. IProductService -> ProductService
    // 2. ProductService -> ProductService
}

// 仅注册实现类（常用于 Window/Page）
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true, SelfOnly = true)]
public partial class MainWindow : Window
{
    // 仅注册 MainWindow -> MainWindow
    // 不注册 Window -> MainWindow
}
```

---

### **中断性变更说明**

#### v3.x → v4.x

1. **异步方法**
   - `ConfigureServices` 和 `ApplicationInitialization` 改为异步
   - 需要返回 `Task`，使用 `await Task.CompletedTask` 结束同步方法

2. **GetEnable 函数**
   - 移除 `Enable` 属性
   - 新增 `GetEnable` 方法，支持运行时动态判断

3. **IHost 统一**
   - `GetApplicationBuilder()` 已弃用
   - 使用 `GetApplicationHost()` 并根据平台转换类型

---

### **常见问题 (FAQ)**

#### Q: 如何在模块中使用配置？

```csharp
var config = context.ServiceProvider.GetConfiguration();
var connectionString = config.GetConnectionString("Default");
```

#### Q: 如何在运行时获取 Scoped 服务？

```csharp
// 方式 1: 使用 IServiceScopeFactory
var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
using var scope = scopeFactory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IScopedService>();

// 方式 2: 使用 Resolver
using var resolver = provider.CreateResolver(createScope: true);
var service = resolver.Resolve<IScopedService>();
```

#### Q: 模块的初始化顺序是怎样的？

按照 `DependsOn` 的声明顺序，依赖模块先执行：

1. 执行所有模块的 `ConfigureServices`（按依赖顺序）
2. 构建 ServiceProvider
3. 执行所有模块的 `ApplicationInitialization`（按依赖顺序）

#### Q: 如何禁用某个模块？

重写 `GetEnable` 方法返回 `false`：

```csharp
public override bool GetEnable(ConfigureServicesContext context) => false;
```

---

### **性能优化建议**

1. **缓存构造函数信息**: Resolver 已内置构造函数缓存，避免重复反射
2. **合理使用作用域**: 避免在 Singleton 中注入 Scoped 服务
3. **延迟初始化**: 不需要的模块通过 `GetEnable` 返回 false 禁用
4. **异步操作**: 充分利用异步方法，避免阻塞初始化

---

### **技术支持**

- 📖 示例项目: [WPF](https://github.com/joesdu/WpfAutoDISample) | [WinForms](https://github.com/joesdu/WinFormAutoDISample) | [WinUI3](https://github.com/joesdu/WinUIAutoDISample)
- 🐛 问题反馈: [GitHub Issues](https://github.com/joesdu/EasilyNET/issues)
- 💡 功能建议: 欢迎提交 Pull Request
