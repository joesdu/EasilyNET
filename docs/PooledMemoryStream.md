# PooledMemoryStream 使用文档

## 简介

`PooledMemoryStream` 是一个高性能、可扩展的内存流实现，基于 `ArrayPool<byte>` 池化机制，适用于频繁分配和释放大块内存的场景。它实现了 `Stream` 抽象，并支持高效的读写、扩容、转数组等操作，能有效减少 GC 压力，提升内存利用率。

---

## 适用场景

- **高频率创建和销毁内存流**：如 Web API、序列化/反序列化、网络通信等场景，避免频繁分配大数组带来的 GC 压力。
- **大数据量内存操作**：如文件处理、图片/音视频流处理、缓存等，数据量大时可显著减少内存碎片。
- **对性能和内存敏感的服务端应用**：如高并发服务、微服务、消息队列等。
- **需要与 `Stream` 兼容的接口**：可直接替换 `MemoryStream` 用于大部分流操作。

---

## 不适用场景

- **极小数据量且生命周期极短的流**：如只存储几十字节的小对象，`MemoryStream` 的分配开销可忽略。
- **多线程并发访问同一个流实例**：本类设计用于单线程使用，与 `MemoryStream`、`FileStream` 等 .NET 流类型保持一致。如需多线程访问，请使用外部锁保护。
- **需要持久化或跨进程共享的流**：本类仅适合进程内内存操作。

---

## 线程安全说明

`PooledMemoryStream` **不是线程安全的**，这遵循了 .NET 流类型（如 `MemoryStream`、`FileStream`）的设计惯例。

**为什么不提供内置线程安全？**

`IBufferWriter<byte>` 接口的使用模式（`GetSpan()` → 写入 → `Advance()`）从根本上无法实现线程安全：

- `GetSpan()`/`GetMemory()` 返回的是对内部缓冲区的直接引用
- 如果在使用返回的 `Span`/`Memory` 期间，另一个线程触发了缓冲区扩容，原引用将指向已归还给池的旧数组（use-after-free）
- 即使在方法内部加锁，锁也会在返回引用时释放，无法保护后续操作

**如果需要多线程访问，请使用外部同步：**

```csharp
private readonly Lock _lock = new();
private readonly PooledMemoryStream _stream = new();

public void ThreadSafeWrite(byte[] data)
{
    lock (_lock)
    {
        _stream.Write(data);
    }
}
```

---

## 主要特性

- **自动扩容**：写入超出容量时自动扩容，扩容策略高效。
- **池化内存**：底层数组来自 `ArrayPool<byte>`，释放时归还池，减少 GC。
- **支持 `Span<byte>`/`Memory<byte>`**：高效的无分配读写。
- **与 `Stream` API 兼容**：可用于所有需要 `Stream` 的场景。
- **Dispose 安全**：释放后自动归还内存，防止重复归还。

---

## 典型用法

### 1. 基本写入与读取

```csharp
using var stream = new PooledMemoryStream();
byte[] data = { 1, 2, 3, 4, 5 };
stream.Write(data, 0, data.Length);

stream.Position = 0;
byte[] buffer = new byte[data.Length];
int read = stream.Read(buffer, 0, buffer.Length);
// buffer 现在包含 [1,2,3,4,5]
```

### 2. 使用 Span/Memory 高效操作

```csharp
using var stream = new PooledMemoryStream();
Span<byte> span = stackalloc byte[100];
stream.Write(span);

stream.Position = 0;
Span<byte> readSpan = stackalloc byte[100];
stream.Read(readSpan);
```

### 3. 转为字节数组或 ArraySegment

```csharp
byte[] arr = stream.ToArray(); // 拷贝数据
ArraySegment<byte> seg = stream.ToArraySegment(); // 只读视图（注意生命周期）
```

### 4. 写入到其他流

```csharp
using var file = File.OpenWrite("output.bin");
stream.WriteTo(file);
```

### 5. 指定初始容量或自定义池

```csharp
var pool = ArrayPool<byte>.Create();
using var stream = new PooledMemoryStream(pool, 4096);
```

---

## 注意事项

- **必须调用 `Dispose()` 或使用 `using` 释放流**，否则底层数组不会归还池，可能导致内存泄漏。
- **单线程使用**，这与 `MemoryStream`、`FileStream` 等 .NET 流类型保持一致。如需多线程请自行加锁。
- **Dispose 后访问会抛出 `ObjectDisposedException`**。
- **`GetSpan()`/`GetMemory()` 返回的引用在缓冲区扩容后失效**：调用 `Write`、`SetLength` 等可能扩容的方法后，之前获取的 `Span`/`Memory` 不再有效。
- **ToArray/ToArraySegment 返回的数据仅在流未 Dispose 前有效**。
- **扩容时会分配更大的数组并拷贝原数据，极大数据量下建议预估容量**。

---

## 性能对比

- 在大数据量和高频场景下，`PooledMemoryStream` 比 `MemoryStream` 更节省内存分配和 GC 时间。
- 小数据量或极短生命周期场景下，两者性能差异不大。

---

## 总结

**推荐在需要频繁创建/销毁大内存流、对 GC 敏感、或有高性能需求的场景下使用 `PooledMemoryStream`。**  
如仅偶尔用到小内存流，`MemoryStream` 也完全可用。
