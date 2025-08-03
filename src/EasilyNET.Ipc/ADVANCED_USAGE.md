# IPC 高级命令系统使用指南

## 概述

新的 IPC 命令系统提供了类型安全的命令处理机制，自动序列化/反序列化，以及基于接口的命令分发。

## 主要特性

1. **类型安全**: 通过泛型接口确保命令和响应的类型安全
2. **自动序列化**: 无需手动处理序列化和反序列化
3. **命令注册**: 自动管理命令类型和处理器的映射关系
4. **易于扩展**: 支持自定义序列化器和处理器

## 快速开始

### 1. 定义命令负载

```csharp
public class CreateUserPayload
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}
```

### 2. 定义命令

```csharp
public class CreateUserCommand : IpcCommandBase<CreateUserPayload>
{
    public CreateUserCommand(CreateUserPayload payload, string? targetId = null)
        : base(payload, targetId)
    {
    }
}
```

### 3. 定义响应数据

```csharp
public class CreateUserResponse
{
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### 4. 实现命令处理器

```csharp
public class CreateUserCommandHandler : IIpcCommandHandler<CreateUserCommand, CreateUserPayload, CreateUserResponse>
{
    public async Task<IpcCommandResponse<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
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

### 5. 注册服务

```csharp
services.AddAdvancedIpc()
    .AddIpcCommandHandler<CreateUserCommand, CreateUserPayload, CreateUserResponse, CreateUserCommandHandler>();
```

### 6. 使用命令

```csharp
// 创建命令
var payload = new CreateUserPayload
{
    UserName = "张三",
    Email = "zhangsan@example.com",
    Age = 25
};
var command = new CreateUserCommand(payload);

// 序列化命令
var serializer = serviceProvider.GetService<IIpcGenericSerializer>();
var registry = serviceProvider.GetService<IpcCommandRegistry>();
var data = serializer.SerializeCommand(command, registry);

// 反序列化命令
var deserializedCommand = serializer.DeserializeCommand(data, registry);

// 分发命令
var dispatcher = serviceProvider.GetService<IpcCommandDispatcher>();
var response = await dispatcher.DispatchAsync(deserializedCommand);
```

## 与旧版本的对比

### 旧版本问题

1. **字符串命令类型**: 容易出错，难以维护
2. **手动序列化**: 需要用户自己处理序列化逻辑
3. **类型不安全**: 缺乏编译时类型检查

### 新版本优势

1. **类型安全**: 编译时检查，减少运行时错误
2. **自动序列化**: 框架自动处理，减少样板代码
3. **易于维护**: 基于接口的设计，便于扩展和测试

## 扩展点

### 自定义序列化器

```csharp
public class MyCustomSerializer : IIpcGenericSerializer
{
    // 实现序列化逻辑
}

// 注册自定义序列化器
services.AddSingleton<IIpcGenericSerializer, MyCustomSerializer>();
```

### 自定义命令基类

```csharp
public abstract class MyCommandBase<TPayload> : IpcCommandBase<TPayload>, IHasCommandTypeName
{
    protected MyCommandBase(TPayload payload, string? targetId = null)
        : base(payload, targetId)
    {
    }

    public abstract string CommandTypeName { get; }
}
```

## 最佳实践

1. **命名规范**: 使用清晰的命令和负载命名
2. **版本管理**: 为命令指定版本号以支持向后兼容
3. **错误处理**: 在处理器中妥善处理异常
4. **日志记录**: 记录关键操作和错误信息
5. **测试**: 为命令处理器编写单元测试
