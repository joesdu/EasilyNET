# IPC 系统迁移指南

## 从旧系统迁移到新系统

### 概述

新的 IPC 系统提供了更好的类型安全性、自动序列化和更清晰的架构。本指南将帮助您从旧的基于字符串的命令系统迁移到新的泛型命令系统。

### 主要变化

1. **命令类型**: 从字符串转为类型安全的类
2. **序列化**: 从手动处理转为自动序列化
3. **处理器**: 从通用处理器转为类型化处理器

### 迁移步骤

#### 步骤 1: 重构命令定义

**旧版本**:

```csharp
var command = new IpcCommand
{
    CommandType = "CreateUser",
    Payload = JsonSerializer.Serialize(new { UserName = "张三", Email = "test@example.com" })
};
```

**新版本**:

```csharp
// 1. 定义负载类型
public class CreateUserPayload
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 2. 定义命令类型
public class CreateUserCommand : IpcCommandBase<CreateUserPayload>
{
    public CreateUserCommand(CreateUserPayload payload, string? targetId = null)
        : base(payload, targetId)
    {
    }
}

// 3. 创建命令
var payload = new CreateUserPayload { UserName = "张三", Email = "test@example.com" };
var command = new CreateUserCommand(payload);
```

#### 步骤 2: 重构处理器

**旧版本**:

```csharp
public class UserCommandHandler : IIpcCommandHandler
{
    public async Task<IpcCommandResponse> HandleCommandAsync(IpcCommand command)
    {
        switch (command.CommandType)
        {
            case "CreateUser":
                var userData = JsonSerializer.Deserialize<UserData>(command.Payload);
                // 处理逻辑
                break;
            // 其他命令...
        }
    }
}
```

**新版本**:

```csharp
// 1. 定义响应类型
public class CreateUserResponse
{
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// 2. 实现类型化处理器
public class CreateUserCommandHandler : IIpcCommandHandler<CreateUserCommand, CreateUserPayload, CreateUserResponse>
{
    public async Task<IpcCommandResponse<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // 直接访问强类型的负载数据
        var userInfo = command.Payload;

        // 处理逻辑
        var response = new CreateUserResponse
        {
            UserId = Ulid.NewUlid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        return IpcCommandResponse<CreateUserResponse>.Success(
            command.CommandId, response, "用户创建成功");
    }
}
```

#### 步骤 3: 更新服务注册

**旧版本**:

```csharp
services.AddSingleton<IIpcCommandHandler, UserCommandHandler>();
```

**新版本**:

```csharp
services.AddAdvancedIpc()
    .AddIpcCommandHandler<CreateUserCommand, CreateUserPayload, CreateUserResponse, CreateUserCommandHandler>();
```

#### 步骤 4: 更新应用初始化

**旧版本**:

```csharp
var app = builder.Build();
```

**新版本**:

```csharp
var app = builder.Build();
app.InitializeIpc(); // 初始化 IPC 服务
```

### 并行迁移策略

如果您需要逐步迁移，可以同时支持两套系统：

1. **保留旧的处理器**: 继续处理旧格式的命令
2. **添加新的处理器**: 处理新格式的命令
3. **创建适配器**: 在新旧系统之间转换

```csharp
public class LegacyCommandAdapter : IIpcCommandHandler
{
    private readonly IpcCommandDispatcher _dispatcher;

    public async Task<IpcCommandResponse> HandleCommandAsync(IpcCommand command)
    {
        // 将旧命令转换为新命令
        var newCommand = ConvertToNewCommand(command);

        // 使用新的分发器处理
        return await _dispatcher.DispatchAsync(newCommand);
    }
}
```

### 最佳实践

1. **分阶段迁移**: 一次迁移一个命令类型
2. **保持测试**: 确保迁移后功能正常
3. **文档更新**: 更新 API 文档和使用指南
4. **向后兼容**: 在完全迁移之前保持对旧格式的支持

### 迁移检查清单

- [ ] 定义新的负载类型
- [ ] 创建新的命令类
- [ ] 实现新的处理器
- [ ] 更新服务注册
- [ ] 初始化 IPC 服务
- [ ] 测试新功能
- [ ] 更新文档
- [ ] 移除旧代码（可选）

### 常见问题

**Q: 新系统是否支持自定义序列化器？**
A: 是的，您可以实现 `IIpcGenericSerializer` 接口来提供自定义序列化逻辑。

**Q: 如何处理版本兼容性？**
A: 在注册命令时可以指定版本号，系统会自动处理版本信息。

**Q: 是否可以继续使用旧的 IpcCommand？**
A: 可以，但建议尽快迁移到新系统，旧系统将在未来版本中移除。

**Q: 新系统的性能如何？**
A: 新系统通过减少字符串比较和提供更好的缓存机制，性能会有所提升。
