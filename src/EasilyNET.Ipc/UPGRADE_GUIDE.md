# IPC 复杂数据类型支持升级说明

## 概述

本次升级为 EasilyNET.Ipc 库添加了对复杂数据类型的全面支持，允许使用不同的序列化组件处理非字符串格式的负载数据。

## 主要改进

### 1. 数据模型升级

#### IpcCommand 类更新
- **新增 `PayloadBytes` 属性**: `ReadOnlyMemory<byte>` 类型，支持二进制数据
- **保留 `Payload` 属性**: 标记为 `[Obsolete]` 以保持向后兼容性
- **添加帮助方法**: 
  - `SetPayload(string data)` - 设置字符串负载
  - `SetPayload(ReadOnlyMemory<byte> data)` - 设置二进制负载
  - `SetPayload<T>(T data)` - 设置对象负载（JSON 序列化）
  - `GetPayloadAsString()` - 获取字符串负载
  - `GetPayloadAsBytes()` - 获取二进制负载
  - `GetPayloadAs<T>()` - 获取反序列化对象

#### IpcCommandResponse 类更新
- **新增 `DataBytes` 属性**: `ReadOnlyMemory<byte>` 类型，支持二进制响应数据
- **保留 `Data` 属性**: 标记为 `[Obsolete]` 以保持向后兼容性
- **添加相应的帮助方法**: 与 IpcCommand 类似的 API

### 2. 序列化器增强

#### 现有序列化器
- **JsonIpcSerializer**: 继续支持 JSON 序列化，自动处理新的二进制属性

#### 新增序列化器
- **MessagePackIpcSerializer**: 
  - 高性能二进制序列化
  - 更小的数据包大小
  - 支持 LZ4 压缩
  - 适用于高性能 IPC 场景

### 3. 辅助工具

#### IpcDataHelper 扩展方法
```csharp
// JSON 序列化支持
command.SetJsonPayload(complexObject);
var data = command.GetJsonPayload<MyClass>();

// 二进制数据支持
command.SetBinaryPayload(binaryData);
var binary = command.GetBinaryPayload();

// 便捷创建方法
var command = IpcDataHelper.CreateCommand("CommandType", complexObject);
var response = IpcDataHelper.CreateResponse("cmd-id", responseData);
```

## 使用示例

### 1. 发送复杂对象（JSON）
```csharp
var userData = new UserData 
{ 
    Name = "张三", 
    Age = 30, 
    Roles = ["Admin", "User"] 
};

// 方式 1: 使用扩展方法
var command = new IpcCommand { CommandType = "CreateUser" };
command.SetJsonPayload(userData);

// 方式 2: 使用帮助方法
var command = IpcDataHelper.CreateCommand("CreateUser", userData);
```

### 2. 发送复杂对象（MessagePack）
```csharp
var command = new IpcCommand { CommandType = "ProcessUser" };
var messagePackData = MessagePackSerializer.Serialize(userData);
command.PayloadBytes = messagePackData;

// 使用 MessagePack 序列化器
var serializer = new MessagePackIpcSerializer();
var serializedCommand = serializer.SerializeCommand(command);
```

### 3. 处理二进制数据
```csharp
var fileData = File.ReadAllBytes("image.png");
var command = new IpcCommand { CommandType = "UploadFile" };
command.SetBinaryPayload(fileData);

// 接收端
var receivedData = command.GetBinaryPayload();
File.WriteAllBytes("received_image.png", receivedData.ToArray());
```

### 4. 向后兼容性
```csharp
// 旧代码仍然有效（但会显示过时警告）
command.Payload = JsonSerializer.Serialize(data);
var oldData = command.Payload;

// 新旧混用
var payloadBytes = command.PayloadBytes; // 访问二进制数据
var payloadString = command.Payload;     // 访问字符串数据（自动转换）
```

## 性能优势

### MessagePack vs JSON
- **数据大小**: MessagePack 通常比 JSON 小 20-50%
- **序列化速度**: MessagePack 序列化速度通常快 2-5 倍
- **反序列化速度**: MessagePack 反序列化速度通常快 3-7 倍
- **压缩支持**: 内置 LZ4 压缩算法

### 二进制数据处理
- **零拷贝**: 使用 `ReadOnlyMemory<byte>` 避免不必要的数据拷贝
- **内存效率**: 直接处理二进制数据，无需字符串转换开销
- **类型安全**: 编译时类型检查，运行时性能优化

## 兼容性说明

### 向后兼容
- 所有现有代码无需修改即可正常工作
- `Payload` 和 `Data` 属性标记为过时但仍然可用
- 新旧属性之间自动同步转换

### 迁移建议
1. **逐步迁移**: 可以逐步将 `Payload` 替换为 `PayloadBytes`
2. **性能敏感场景**: 优先使用 MessagePack 序列化器
3. **二进制数据**: 直接使用新的二进制 API
4. **新项目**: 直接使用新的 API 设计

## 最佳实践

1. **选择合适的序列化器**:
   - 人类可读性重要 → JSON
   - 性能和大小重要 → MessagePack
   - 已有 JSON 架构 → JSON
   - 新项目高性能需求 → MessagePack

2. **数据类型选择**:
   - 简单字符串 → 继续使用 `Payload`（考虑迁移）
   - 复杂对象 → 使用 `PayloadBytes` + 序列化
   - 二进制文件 → 直接使用 `PayloadBytes`
   - 混合场景 → 使用扩展方法

3. **错误处理**:
   - 反序列化时处理可能的异常
   - 验证数据完整性
   - 提供合理的默认值

## 注意事项

1. **序列化器一致性**: 客户端和服务端必须使用相同的序列化器
2. **数据版本控制**: 考虑数据结构的版本兼容性
3. **内存管理**: 大数据量时注意内存使用
4. **安全性**: 反序列化时要注意安全性问题

## 总结

此次升级完全保持向后兼容的同时，为 IPC 库添加了强大的复杂数据类型支持。通过引入二进制数据处理和多种序列化器，显著提升了库的灵活性和性能，满足了不同场景下的需求。