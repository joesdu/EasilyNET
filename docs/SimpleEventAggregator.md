# SimpleEventAggregator 使用说明

## 简介

`SimpleEventAggregator` 是一个轻量级的事件聚合器（事件总线），用于解耦多个对象之间的通信。它允许对象（发布者）发送消息，而无需直接引用接收者（订阅者），实现松耦合架构。该实现类似于 WPF 的 `IMessenger`，但更为通用，适用于任何 .NET 应用。

---

## 适用场景

- **MVVM 架构下的消息通信**：如 WPF、WinForms、MAUI、Blazor 等 UI 框架中，ViewModel 之间解耦通信。
- **模块化/插件化系统**：各模块间无需直接依赖，通过事件聚合器传递消息。
- **领域事件/业务事件分发**：如领域驱动设计（DDD）中的领域事件发布与订阅。
- **跨层通信**：如服务层与应用层、UI 层之间的消息传递。
- **解耦第三方库与主业务逻辑**：如日志、通知、状态变更等场景。

---

## 主要特性

- 支持泛型消息类型，类型安全。
- 支持弱引用订阅，防止内存泄漏。
- 支持强引用订阅，适合生命周期一致的场景。
- 线程安全，适合多线程环境。
- 自动清理已回收的订阅者。

---

## 典型用法

### 1. 注册消息接收者

```csharp
var aggregator = new SimpleEventAggregator();
// 注册，默认弱引用
aggregator.Register<MyMessage>(this, msg => Console.WriteLine(msg.Content));
// 强引用注册
aggregator.Register<MyMessage>(this, msg => ..., keepSubscriberReferenceAlive: true);
```

### 2. 发送消息

```csharp
aggregator.Send(new MyMessage { Content = "Hello" });
```

### 3. 注销接收者

```csharp
// 注销某类型消息
aggregator.Unregister<MyMessage>(this);
// 注销所有消息
aggregator.Unregister(this);
```

### 4. 释放资源

```csharp
aggregator.Dispose();
```

---

## 注意事项

- **弱引用注册**（默认）可防止内存泄漏，但接收者被 GC 回收后将自动失效。
- **强引用注册**需确保手动注销，否则可能导致内存泄漏。
- **线程安全**，但回调方法本身需自行保证线程安全。
- **Dispose 后不可再使用**，否则会抛出异常。
- **不适合高频率、低延迟的实时消息场景**，如游戏主循环、超高性能消息队列等。
- **不建议用于进程间通信**，仅适合进程内对象间通信。

---

## 总结

`SimpleEventAggregator` 适合需要解耦、灵活、类型安全的进程内消息通信场景。推荐在 MVVM、模块化、领域事件等架构中使用。如需更复杂的事件总线（如支持异步、过滤、优先级等），可考虑扩展或使用专业库。
