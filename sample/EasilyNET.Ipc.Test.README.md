# EasilyNET.Ipc 测试项目

## 项目说明

本示例包含两个项目用于全面测试 EasilyNET.Ipc 框架的功能：

### 1. EasilyNET.Ipc.Server.Sample - IPC 服务端
- 演示 IPC 服务端的配置和使用
- 包含多种类型的命令处理器
- 支持用户管理、订单处理、系统信息等场景
- 演示不同的序列化方式和配置选项

### 2. EasilyNET.Ipc.Client.Sample - IPC 客户端  
- 演示 IPC 客户端的配置和使用
- 测试各种命令的发送和响应
- 包含性能测试和错误处理测试
- 演示重试策略和熔断器功能

## 功能测试覆盖

### 基础功能
- [x] 跨平台支持 (Windows Named Pipes / Linux Unix Sockets)
- [x] 强类型命令和响应
- [x] JSON 序列化
- [x] 自定义序列化器
- [x] 依赖注入集成

### 高级功能
- [x] 重试策略 (指数退避、线性退避)
- [x] 熔断器机制
- [x] 超时控制
- [x] 连接池管理
- [x] 多传输层实例

### 命令类型测试
- [x] 简单请求-响应模式
- [x] 无响应命令（Fire-and-forget）
- [x] 复杂对象传输
- [x] 大数据传输测试
- [x] 并发请求测试

### 错误处理测试
- [x] 网络异常处理
- [x] 序列化异常处理
- [x] 超时异常处理
- [x] 服务端异常处理

## 创建项目步骤

### 步骤 1: 创建目录结构
```bash
mkdir sample\EasilyNET.Ipc.Server.Sample
mkdir sample\EasilyNET.Ipc.Client.Sample
```

### 步骤 2: 创建项目文件
参考下面提供的完整项目文件内容

### 步骤 3: 运行测试
```bash
# 启动服务端
cd sample\EasilyNET.Ipc.Server.Sample
dotnet run

# 启动客户端（新的终端窗口）
cd sample\EasilyNET.Ipc.Client.Sample  
dotnet run
```

## 测试场景

### 1. 用户管理场景
- 创建用户
- 获取用户信息
- 更新用户信息
- 删除用户

### 2. 订单处理场景
- 创建订单
- 查询订单状态
- 更新订单信息
- 取消订单

### 3. 系统信息场景
- 获取系统状态
- 健康检查
- 性能监控
- 日志收集

### 4. 性能测试场景
- 大量并发请求
- 大数据传输
- 长时间连接测试
- 内存使用测试

## 配置说明

项目支持通过 appsettings.json 配置各种参数：

```json
{
  "Ipc": {
    "PipeName": "EasilyNET_IPC_Test",
    "UnixSocketPath": "/tmp/easilynet_ipc_test.sock",
    "TransportCount": 4,
    "DefaultTimeout": "00:00:30",
    "RetryPolicy": {
      "MaxAttempts": 5,
      "InitialDelay": "00:00:01",
      "BackoffType": "Exponential",
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "MinimumThroughput": 5,
      "BreakDuration": "00:00:30"
    }
  }
}
```