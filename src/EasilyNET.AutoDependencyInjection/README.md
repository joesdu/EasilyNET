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
- 支持 `Lazy<T>` 隐式延迟解析 (调用 `AddApplicationModules` 后自动注册, 首次访问 `.Value` 时才解析 `T`)
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

#### Lazy&lt;T&gt; 隐式延迟解析

调用 `AddApplicationModules` 后, 框架会自动注册开放泛型 `Lazy<>` (使用 `TryAdd`, 不会覆盖你已有的注册)。
直接在构造函数注入 `Lazy<T>` 即可延迟解析, 首次访问 `.Value` 时才从当前作用域解析 `T`, 适用于打破构造期循环依赖或推迟昂贵依赖的创建:

```csharp
public sealed class MyHub(Lazy<IHeavyService> heavy)
{
    public void DoWork() => heavy.Value.Run(); // 首次 .Value 时才解析 IHeavyService
}
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

---

# EasilyNET.AutoDependencyInjection 套件 · 完整使用文档与场景选型指南

> 本节是对 `EasilyNET.AutoDependencyInjection`、`EasilyNET.AutoDependencyInjection.Core` 两个包的**统一汇总**，
> 侧重「有哪些能力 / 什么场景该用哪个 / 怎么选」。上文已给出每个功能的细粒度用法、桌面/Web 集成与 FAQ，本节聚焦**全局视角与选型决策**。

---

## 目录（套件总览）

- [1. 套件结构与选型](#1-套件结构与选型)
- [2. 两大能力：模块化 + 自动注入](#2-两大能力模块化--自动注入)
- [3. 功能 → 场景速查表](#3-功能--场景速查表)
- [4. 最小可用配置（5 分钟上手）](#4-最小可用配置5-分钟上手)
- [5. 模块生命周期与执行顺序](#5-模块生命周期与执行顺序)
- [6. `[DependencyInjection]` 注册策略选型](#6-dependencyinjection-注册策略选型)
- [7. 高级解析器（IResolver）速查](#7-高级解析器iresolver速查)
- [8. 隐式关系类型（Lazy / IIndex / Owned / Func 工厂）](#8-隐式关系类型lazy--iindex--owned--func-工厂)
- [9. 平台与环境矩阵](#9-平台与环境矩阵)
- [10. 常见问题排查](#10-常见问题排查)

---

## 1. 套件结构与选型

| 包 | 角色 | 关键依赖 | 何时引用 |
|---|---|---|---|
| **EasilyNET.AutoDependencyInjection.Core** | 契约层：`[DependencyInjection]`、`[IgnoreDependency]` 两个特性 | 无 | 只想在领域/业务库的类上**打注册特性**、但不引入完整框架时单独引用，避免传递依赖。 |
| **EasilyNET.AutoDependencyInjection** | 实现层：模块系统（`AppModule`/`DependsOn`）、自动注入扫描、`IResolver` 高级解析、`IModuleDiagnostics`、DI 入口扩展 | `Microsoft.Extensions.DependencyInjection`、`Hosting.Abstractions`、`Core` | 真正要启用模块化与自动注入的宿主（Web / Worker / WPF / WinForms / WinUI3）。**主程序装这个即可。** |

- 目标框架：`net10.0`、`net11.0`。
- 不依赖任何 ASP.NET Core 专属 API，可用于 Web / 桌面 / 控制台 / 通用 Host。

```bash
dotnet add package EasilyNET.AutoDependencyInjection        # 主包（含 Core）
# 仅在类库上打注册特性、不引入框架的项目可只引用：
dotnet add package EasilyNET.AutoDependencyInjection.Core
```

---

## 2. 两大能力：模块化 + 自动注入

本套件解决两件事，可单用也可合用：

| 能力 | 解决的问题 | 核心类型 |
|---|---|---|
| **模块化（AppModule）** | 服务注册与中间件配置散落、顺序难管理 | `AppModule` + `[DependsOn]`，拓扑排序、循环依赖检测、按配置启停 |
| **自动注入** | 大量 `services.AddXxx<I, C>()` 样板代码 | `[DependencyInjection]` 特性 + 内置 `DependencyAppModule` 扫描注册 |

> `DependencyAppModule` 是内置模块：根模块 `[DependsOn(typeof(DependencyAppModule))]` 后，所有带 `[DependencyInjection]` 的类自动注册。

---

## 3. 功能 → 场景速查表

| 你的需求 | 用什么 | 入口 |
|---|---|---|
| 自动注册某个服务 | `[DependencyInjection(lifetime)]` | Core 包特性 |
| 排除某类/接口不被扫描 | `[IgnoreDependency]` | Core 包特性 |
| 注册键控服务（多实现） | `ServiceKey` | `[DependencyInjection(..., ServiceKey = "x")]` |
| 同时注册接口 + 自身 / 仅自身 | `AddSelf` / `SelfOnly` | 特性属性（窗口类常用 `AddSelf+SelfOnly`） |
| 指定注册为某基类/接口（提性能） | `AsType` | 特性属性 |
| 按功能域组织注册与中间件 | 自定义 `AppModule` | 重写 `ConfigureServices` 等 |
| 声明模块初始化顺序 | `[DependsOn(...)]` | 拓扑排序，依赖先行 |
| 按配置/环境启停模块 | 重写 `GetEnable(context)` | 读 `context.Configuration` |
| 配置中间件（Web） | 重写 `ApplicationInitialization` | `context.GetApplicationHost()` |
| 应用停止时清理资源 | 重写 `ApplicationShutdown` | 按模块**逆序**调用 |
| 缩小自动扫描范围 | `ConfigureAutoInjectionFilter` | 须在 `AddApplicationModules` 之前 |
| 运行时按参数覆盖构造依赖 | `IResolver` + `Parameter` | `provider.Resolve<T>(...)` |
| 命名/键控解析 | `ResolveNamed` / `ResolveKeyed` | `IResolver` / `IServiceProvider` 扩展 |
| 打破构造期循环依赖 | `Lazy<T>` 隐式延迟 | 直接注入 `Lazy<T>` |
| 运行时传参创建实例 | 参数化工厂 | `AddParameterizedFactory<...>()` |
| 受控生命周期（用完即释放作用域） | `Owned<T>` | `ResolveOwned<T>()` / `AddOwnedFactory<T>()` |
| 查看已加载模块/注册服务/校验依赖 | `IModuleDiagnostics` | 注入后调用 |

---

## 4. 最小可用配置（5 分钟上手）

```csharp
// 1) 业务服务打特性即自动注册（默认 Scoped，注册其实现的接口）
[DependencyInjection(ServiceLifetime.Scoped)]
public class OrderService : IOrderService { /* ... */ }

// 2) 根模块依赖内置 DependencyAppModule（提供自动注入）
[DependsOn(typeof(DependencyAppModule))]
public class AppWebModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddHttpContextAccessor();
    }

    public override Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseAuthorization();
        return Task.CompletedTask;
    }
}

// 3) Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationModules<AppWebModule>();   // 注册模块系统 + 自动注入
var app = builder.Build();
app.InitializeApplication();                              // 执行各模块 ApplicationInitialization
app.MapControllers();
app.Run();
```

> 桌面端（WPF/WinForms/WinUI3）把 `GetApplicationHost()` 转为 `IHost`，窗口类用 `[DependencyInjection(Singleton, AddSelf = true, SelfOnly = true)]`（详见上文桌面集成章节）。

---

## 5. 模块生命周期与执行顺序

```text
AddApplicationModules<T>()
  → 按 [DependsOn] 拓扑排序加载模块（依赖在前，循环依赖直接抛 InvalidOperationException）
  → 每个模块 GetEnable(context)：返回 false 则跳过
  → ConfigureServices(context)        // 同步，99% 注册都在这里
  → ConfigureServicesAsync(context)   // 异步（罕见；实现需 ConfigureAwait(false)）
  → [构建 ServiceProvider]
InitializeApplication() / InitializeApplicationAsync()
  → ApplicationInitializationSync(context)   // 同步中间件配置，先于异步
  → ApplicationInitialization(context)       // 异步中间件配置
ShutdownApplication() / ShutdownApplicationAsync()
  → ApplicationShutdown(context)             // 按模块【逆序】，用于资源清理
```

| 阶段 | 方法 | 同步/异步 | 典型用途 |
|---|---|---|---|
| 启停判定 | `GetEnable` | 同步 | 按配置/环境决定是否加载模块 |
| 注册 | `ConfigureServices` | 同步 | 注册服务（首选） |
| 注册 | `ConfigureServicesAsync` | 异步 | 极少数需异步的初始化 |
| 初始化 | `ApplicationInitializationSync` | 同步 | 同步中间件 |
| 初始化 | `ApplicationInitialization` | 异步 | 中间件 / 应用配置 |
| 关闭 | `ApplicationShutdown` | 异步 | 逆序清理资源 |

> 配置访问推荐 `context.Configuration`（无需构建临时 ServiceProvider）；环境用 `context.Environment`（非托管场景可能为 null）。

---

## 6. `[DependencyInjection]` 注册策略选型

| 想要的效果 | 写法 | 结果 |
|---|---|---|
| 注册接口（默认） | `[DependencyInjection(ServiceLifetime.Scoped)]` | `IFoo → Foo` |
| 接口 + 自身都能取 | `AddSelf = true` | `IFoo → Foo` 且 `Foo → Foo` |
| 只注册自身（窗口/页面） | `AddSelf = true, SelfOnly = true` | 仅 `Foo → Foo`（不注册父类/接口） |
| 多实现按键区分 | `ServiceKey = "redis"` | KeyedService，`ResolveKeyed<ICache>("redis")` |
| 显式指定服务类型 | `AsType = typeof(IFoo)` | 注册为 `IFoo`，减少反射、提性能 |
| 排除扫描 | 类/接口上加 `[IgnoreDependency]` | 不被自动注册（`IDisposable`/`IAsyncDisposable` 自动跳过） |

特性属性默认值：`Lifetime`（构造传入）、`AddSelf=false`、`SelfOnly=false`、`ServiceKey=null`、`AsType=null`。
缩小扫描范围（须在 `AddApplicationModules` 前）：

```csharp
services.ConfigureAutoInjectionFilter(t => t.Assembly == typeof(MyService).Assembly)
        .AddApplicationModules<AppWebModule>();
```

---

## 7. 高级解析器（IResolver）速查

`IResolver` 在标准 `IServiceProvider` 之上提供**构造参数覆盖 + 命名/键控解析**（类 Autofac）。基础解析仍建议直接用 `IServiceProvider`。

```csharp
using var resolver = provider.CreateResolver(createScope: true); // true=独立作用域，由 resolver 释放
var svc = resolver.Resolve<IMyService>(
    new NamedParameter("connectionString", "Server=localhost"), // 按参数名
    new TypedParameter(typeof(ILogger), logger),                // 按类型
    new PositionalParameter(0, "first"),                        // 按位置（0 基）
    new ResolvedParameter((t, n) => n == "id", (sp, t, n) => Guid.NewGuid())); // 自定义谓词

var keyed = provider.ResolveKeyed<ICache>("redis");
var named = provider.ResolveNamed<ICache>("primary");
```

| 方法 | 用途 |
|---|---|
| `Resolve<T>(params Parameter[])` | 解析 + 参数覆盖，失败抛异常 |
| `ResolveKeyed<T>(key, params?)` | 键控解析 |
| `ResolveNamed<T>(name, params?)` | 命名解析 |
| `CreateResolver(createScope)` | 创建 resolver（可选独立作用域） |

`Parameter` 匹配优先级：Named（名）→ Typed（类型/可赋值）→ Positional（位置）→ Resolved（谓词）；都不匹配则回退容器解析。构造函数按「能满足所有参数」贪心择优，元数据已缓存。

---

## 8. 隐式关系类型（Lazy / IIndex / Owned / Func 工厂）

调用 `AddApplicationModules` 后自动注册以下能力（`Lazy<>` 用 `TryAdd`，不覆盖你已有注册）：

```csharp
// 延迟解析：打破构造期循环依赖 / 推迟昂贵依赖
public sealed class Hub(Lazy<IHeavy> heavy) { void Go() => heavy.Value.Run(); }

// 键控索引：按 key 取服务
public sealed class Dispatcher(IIndex<string, IHandler> handlers)
{
    void On(string k) { if (handlers.TryGet(k, out var h)) h!.Handle(); }
}

// 参数化工厂：运行时传参创建（1-4 参强类型，5+ 用 object[]）
services.AddParameterizedFactory<string, IFooService>();                 // Func<string, IFoo>
services.AddParameterizedFactory<MyService>(typeof(string), typeof(int), typeof(bool), typeof(string), typeof(double));

// 受控生命周期：用完释放作用域及其 Scoped 依赖
var owned = provider.ResolveOwned<IScopedService>();
owned.Value.Do();
owned.Dispose();
services.AddOwnedFactory<IScopedService>();   // 注入 Func<Owned<IScopedService>>
```

> 多参工厂内部用 `PositionalParameter` 按位置匹配，参数类型相同也能区分；`object[]` 重载在调用时按 `paramTypes` 做运行时校验，数量/类型不符抛 `ArgumentException`。

---

## 9. 平台与环境矩阵

| 平台 | 获取 Host | 备注 |
|---|---|---|
| ASP.NET Core | `GetApplicationHost() as WebApplication` / `as IApplicationBuilder` | 中间件在 `ApplicationInitialization` 配置 |
| WPF / WinForms / WinUI3 | `GetApplicationHost() as IHost` | 窗口类需 `AddSelf=true, SelfOnly=true` |
| Worker / 控制台 / 通用 Host | `GetApplicationHost() as IHost` | 无中间件管道，仅服务注册与初始化 |

- 桌面应用面向 .NET（非 .NET Framework）。
- 诊断：注入 `IModuleDiagnostics` → `GetLoadedModules()` / `GetAutoRegisteredServices()` / `ValidateModuleDependencies()`。

---

## 10. 常见问题排查

| 现象 | 可能原因 / 解决 |
|---|---|
| `GetRequiredService<MainWindow>()` 取不到 | 窗口类需 `[DependencyInjection(Singleton, AddSelf = true, SelfOnly = true)]`（默认会注册父类 `Window`） |
| 服务没被自动注册 | 类未打 `[DependencyInjection]`；被 `[IgnoreDependency]` 或 `ConfigureAutoInjectionFilter` 过滤；是抽象/接口/开放泛型（会跳过） |
| `Circular dependency detected` | `[DependsOn]` 形成环；改为共同依赖同一基模块，确保依赖图是 DAG |
| 自动注入未生效 | 根模块缺少 `[DependsOn(typeof(DependencyAppModule))]` |
| 过滤器不起作用 | `ConfigureAutoInjectionFilter` 必须在 `AddApplicationModules<T>` **之前**调用 |
| 配置在 `ConfigureServices` 里取不到 | 用 `context.Configuration`（而非构建临时 ServiceProvider） |
| Singleton 注入 Scoped 报错/泄漏 | 不要在 Singleton 直接注入 Scoped；改用 `IServiceScopeFactory`、`Resolver(createScope:true)` 或 `Owned<T>` |
| 中间件顺序不对 | 中间件在各模块 `ApplicationInitialization` 中按模块依赖顺序执行，调整 `[DependsOn]` |
| v4.x 升级后 `ConfigureServices` 编译错误 | v5.x 已改为同步 `void`，移除 `async/await`；异步逻辑挪到 `ConfigureServicesAsync` |