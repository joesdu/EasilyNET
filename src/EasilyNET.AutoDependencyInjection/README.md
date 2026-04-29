#### EasilyNET.AutoDependencyInjection

一个功能强大的自动依赖注入模块系统，提供模块化的服务配置和中间件管理能力。

---

### **核心特性**

#### 1. **模块化架构 (AppModule)**

- 基于 `AppModule` 的模块系统，支持依赖关系声明 (`DependsOn`)
- 模块加载顺序自动解析（拓扑排序），确保依赖模块优先初始化
- 支持通过 `GetEnable` 方法动态控制模块启用/禁用（可从配置文件读取）
- 新增循环依赖检测，防止错误的模块依赖声明

#### 2. **同步/异步分离设计**

- **`ConfigureServices(context)`** - **同步方法**，用于服务注册（99%场景）
- **`ConfigureServicesAsync(context, ct)`** - **异步方法**，用于罕见的异步初始化场景
- **`ApplicationInitializationSync(context)`** - **同步方法**，用于同步的中间件/应用配置，在异步版本之前调用
- **`ApplicationInitialization(context)`** - **异步方法**，用于中间件/应用配置
- **`ApplicationShutdown(context)`** - **异步方法**，应用停止时按模块逆序调用，用于资源清理
- 清晰的生命周期划分，避免死锁风险

#### 3. **KeyedService 支持**

- 完整支持 .NET 的 KeyedService 功能
- 可在 `DependencyInjectionAttribute` 中使用 `ServiceKey` 属性标识服务键值
- 支持通过 `ResolveKeyed<T>(key)` 解析键控服务

#### 4. **高级服务解析器 (IResolver)**

- 提供类似 Autofac 的动态解析能力，同时基于 `Microsoft.Extensions.DependencyInjection`
- 支持构造函数参数覆盖 (NamedParameter, TypedParameter, PositionalParameter, ResolvedParameter)
- 支持命名解析、键控解析
- 支持 `Owned<T>` 受控生命周期管理
- 支持 `IIndex<TKey, TService>` 键控服务索引
- 支持参数化工厂 (`Func<TParam, TService>`、`Func<T1, T2, ..., TService>` 最多 4 参数强类型，以及通用
  `Func<object[], TService>` 支持 5+ 参数)

#### 5. **多平台支持**

- **Web 应用**: ASP.NET Core (WebApplication, IApplicationBuilder)
- **桌面应用**: WPF, WinForms, WinUI3 (.NET 项目，不支持 .NET Framework)
- 统一的 API 接口，便于跨平台项目复用模块

#### 6. **模块诊断 API**

- 新增 `IModuleDiagnostics` 接口
- 查看已加载模块及其执行顺序
- 查看自动注册的服务列表
- 验证模块依赖关系

---

### **示例项目**

| 平台       | 示例项目                                                         | 状态     |
|----------|--------------------------------------------------------------|--------|
| WPF      | [WPF 示例](https://github.com/joesdu/WpfAutoDISample)          | ✅ 最新   |
| WinForms | [WinForms 示例](https://github.com/joesdu/WinFormAutoDISample) | ✅ 最新   |
| WinUI3   | [WinUI3 示例](https://github.com/joesdu/WinUIAutoDISample)     | ⚠️ 待更新 |

---

### **Resolver 高级解析器**

`IResolver` 提供比原生 `IServiceProvider` 更强大的服务解析能力。

#### 核心方法

| 方法                               | 说明                   |
|----------------------------------|----------------------|
| `Resolve<T>(params Parameter[])` | 解析服务（支持参数覆盖），失败抛异常   |
| `ResolveKeyed<T>(key, params?)`  | 解析键控服务（KeyedService） |
| `ResolveNamed<T>(name, params?)` | 解析命名服务               |

#### 构造函数参数注入

支持四种参数类型：

1. **NamedParameter**: 按参数名匹配
2. **TypedParameter**: 按参数类型匹配
3. **PositionalParameter**: 按参数位置匹配（零基索引）
4. **ResolvedParameter**: 自定义匹配逻辑和值提供

#### 使用示例

```csharp
// 1. 基本解析
using var resolver = provider.CreateResolver();
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

// 4. 命名服务解析
var namedService = resolver.ResolveNamed<ICache>("primary");

// 5. 受控生命周期（Owned<T>）
var owned = provider.ResolveOwned<IScopedService>();
// 使用完毕后释放作用域
owned.Dispose();

// 6. 创建独立作用域的 Resolver
using var scopedResolver = provider.CreateResolver(createScope: true);
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

#### 参数化工厂 (Func<..., TService>)

支持将参数化工厂注册为 `Func` 委托，注入后可在运行时传参创建服务实例：

```csharp
// 1-4 参数：强类型重载
services.AddParameterizedFactory<string, IFooService>();                          // Func<string, IFoo>
services.AddParameterizedFactory<string, int, IBarService>();                     // Func<string, int, IBar>
services.AddParameterizedFactory<string, int, bool, IBazService>();               // Func<string, int, bool, IBaz>
services.AddParameterizedFactory<string, int, bool, string, IQuxService>();       // Func<string, int, bool, string, IQux>

// 5+ 参数：通用兜底（Func<object[], TService>），需声明参数类型用于运行时校验
services.AddParameterizedFactory<IComplexService>(
    typeof(string), typeof(int), typeof(bool), typeof(string), typeof(double), typeof(string));

// 使用示例
public class MyController(
    Func<string, IFooService> fooFactory,
    Func<object[], IComplexService> complexFactory)
{
    public void Do()
    {
        var foo = fooFactory("hello");
        var complex = complexFactory(["a", 1, false, "b", 2.5, "c"]);
    }
}

// Owned 工厂：每次调用创建独立作用域
services.AddOwnedFactory<IScopedService>();
// 注入 Func<Owned<IScopedService>>，调用后需手动 Dispose
```

> **注意**：多参数工厂内部使用 `PositionalParameter` 按位置匹配，即使多个参数类型相同也能正确区分。

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
    // 同步服务注册（推荐）
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        // 注册应用服务
        context.Services.AddSingleton<IMyService, MyService>();
    }
    
    // 可选：异步初始化
    public override Task ConfigureServicesAsync(ConfigureServicesContext context, CancellationToken ct)
    {
        // 罕见的异步初始化场景
        return Task.CompletedTask;
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
        var config = context.Configuration;  // 直接使用 context.Configuration
        return config.GetSection("ServicesEnable").GetValue<bool>("Cors");
    }

    // 同步服务注册（推荐）
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Configuration;  // 直接使用 context.Configuration
        var allow = config["AllowedHosts"] ?? "*";

        context.Services.AddCors(c =>
            c.AddPolicy("AllowedHosts", s =>
                s.WithOrigins(allow.Split(","))
                 .AllowAnyMethod()
                 .AllowAnyHeader()));
    }

    // 配置中间件（异步）
    public override Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseCors("AllowedHosts");
        return Task.CompletedTask;
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
    // 同步服务注册
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddHttpContextAccessor();
        // 其他服务注册
    }
    
    // 可选：异步初始化
    public override Task ConfigureServicesAsync(ConfigureServicesContext context, CancellationToken ct)
    {
        // 罕见的异步初始化场景
        return Task.CompletedTask;
    }

    // 应用初始化（异步）
    public override Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseAuthorization();
        // 其他中间件配置
        return Task.CompletedTask;
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

模块的 `DependsOn` 顺序决定了初始化顺序。被依赖的模块会先执行（使用拓扑排序算法）：

```csharp
[DependsOn(
    typeof(DependencyAppModule),    // 第 1 个初始化
    typeof(DatabaseModule),         // 第 2 个初始化（DependencyAppModule 完成后）
    typeof(CachingModule),          // 第 3 个初始化
    typeof(AuthenticationModule)    // 第 4 个初始化
)]
public class AppWebModule : AppModule  // 最后初始化
{
    // ...
}
```

**重要**：如果 `DatabaseModule` 依赖 `DependencyAppModule`，则 `DependencyAppModule` 会先执行，然后是 `DatabaseModule`
，无论它们在 `DependsOn` 中的声明顺序如何。

#### 模块职责划分

建议按功能领域划分模块：

```csharp
// 数据库模块
public class DatabaseModule : AppModule
{
    // 同步服务注册
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var connectionString = context.Configuration
            .GetConnectionString("Default");
        context.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}

// 认证模块
public class AuthenticationModule : AppModule
{
    // 同步服务注册
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options => { /* ... */ });
    }

    // 异步中间件配置
    public override Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseAuthentication();
        app?.UseAuthorization();
        return Task.CompletedTask;
    }
}

// Swagger 文档模块
public class SwaggerModule : AppModule
{
    public override bool GetEnable(ConfigureServicesContext context)
    {
        // 使用 context.Configuration 直接访问配置
        return context.Configuration.GetValue<bool>("Swagger:Enabled");
    }

    // 同步服务注册
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddSwaggerGen();
    }

    // 异步中间件配置
    public override Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseSwagger();
        app?.UseSwaggerUI();
        return Task.CompletedTask;
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
    // 推荐使用 context.Configuration
    return context.Configuration
        .GetSection("ServicesEnable")
        .GetValue<bool>("Swagger");
}
```

---

### **DependencyInjection 特性说明**

#### 特性属性

| 属性           | 类型              | 说明                                 | 默认值    |
|--------------|-----------------|------------------------------------|--------|
| `Lifetime`   | ServiceLifetime | 服务生命周期（Singleton/Scoped/Transient） | Scoped |
| `ServiceKey` | object?         | 键控服务的键值（KeyedService）              | null   |
| `AddSelf`    | bool            | 是否注册实现类自身                          | false  |
| `SelfOnly`   | bool            | 是否仅注册实现类（不注册接口）                    | false  |

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

#### v4.x → v5.x

1. **ConfigureServices 改为同步方法**
    - `ConfigureServices` 从 `async Task` 改为 `void`
    - 原因：服务注册阶段本身应该是同步的，避免死锁风险
    - 迁移：移除 `async/await` 关键字，删除 `await Task.CompletedTask`

2. **新增 ConfigureServicesAsync 方法**
    - 如需在服务注册阶段执行异步操作，重写 `ConfigureServicesAsync`
    - 此方法在 `ConfigureServices` 之后调用

3. **配置访问方式变更**
    - 旧：`context.ServiceProvider.GetConfiguration()`
    - 新：`context.Configuration`（推荐）或 `context.ServiceProvider.GetRequiredService<IConfiguration>()`

4. **IStartupModuleRunner.Initialize 签名变更**
    - 旧：`void Initialize()`
    - 新：`void Initialize(IServiceProvider serviceProvider)`

5. **新增循环依赖检测**
    - 如果模块存在循环依赖，将抛出 `InvalidOperationException`
    - 确保模块依赖形成有向无环图（DAG）

6. **新增 IModuleDiagnostics 接口**
    - 可注入 `IModuleDiagnostics` 查看模块加载情况
    - 支持查看模块执行顺序、自动注册服务列表

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
// 推荐方式（新）
public override void ConfigureServices(ConfigureServicesContext context)
{
    var config = context.Configuration;
    var connectionString = config.GetConnectionString("Default");
}

// 兼容方式（旧，仍可用）
public override void ConfigureServices(ConfigureServicesContext context)
{
    var config = context.ServiceProvider.GetRequiredService<IConfiguration>();
}
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

使用拓扑排序算法，确保依赖模块先执行：

1. 执行所有模块的 `ConfigureServices`（按依赖顺序，依赖项优先）
2. 执行所有模块的 `ConfigureServicesAsync`（按依赖顺序）
3. 构建 ServiceProvider
4. 执行所有模块的 `ApplicationInitializationSync`（按依赖顺序，同步）
5. 执行所有模块的 `ApplicationInitialization`（按依赖顺序，异步）
6. 应用停止时，执行所有模块的 `ApplicationShutdown`（按依赖**逆序**）

#### Q: 如何查看模块加载情况？

```csharp
// 注入 IModuleDiagnostics
public class MyService
{
    private readonly IModuleDiagnostics _diagnostics;
    
    public MyService(IModuleDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
        
        // 获取已加载模块
        var modules = _diagnostics.GetLoadedModules();
        foreach (var module in modules)
        {
            Console.WriteLine($"{module.Order}: {module.Name}");
        }
        
        // 验证依赖关系
        var issues = _diagnostics.ValidateModuleDependencies();
        if (issues.Count > 0)
        {
            foreach (var issue in issues)
            {
                Console.WriteLine($"警告: {issue}");
            }
        }
    }
}
```

#### Q: 如何禁用某个模块？

重写 `GetEnable` 方法返回 `false`：

```csharp
public override bool GetEnable(ConfigureServicesContext context) => false;
```

#### Q: 出现 "Circular dependency detected" 错误怎么办？

检查 `DependsOn` 声明，确保没有循环依赖：

```csharp
// ❌ 错误：循环依赖
[DependsOn(typeof(ModuleB))]
public class ModuleA : AppModule { }

[DependsOn(typeof(ModuleA))]  // 循环依赖！
public class ModuleB : AppModule { }

// ✅ 正确：无循环依赖
[DependsOn(typeof(BaseModule))]
public class ModuleA : AppModule { }

[DependsOn(typeof(BaseModule))]
public class ModuleB : AppModule { }
```

---

### **性能优化建议**

1. **缓存构造函数信息**: Resolver 已内置构造函数缓存，避免重复反射
2. **合理使用作用域**: 避免在 Singleton 中注入 Scoped 服务
3. **延迟初始化**: 不需要的模块通过 `GetEnable` 返回 false 禁用
4. **同步注册优先**: 使用 `ConfigureServices` 而非 `ConfigureServicesAsync`，除非确实需要异步操作
5. **避免过早 BuildServiceProvider**: 框架已优化，通常不需要手动构建

---

### **技术支持**

- 📖
  示例项目: [WPF](https://github.com/joesdu/WpfAutoDISample) | [WinForms](https://github.com/joesdu/WinFormAutoDISample) | [WinUI3](https://github.com/joesdu/WinUIAutoDISample)
- 🐛 问题反馈: [GitHub Issues](https://github.com/joesdu/EasilyNET/issues)
- 💡 功能建议: 欢迎提交 Pull Request