# Essentials

## RangeStream

`RangeStream` 用于限定读取长度的只读包装流。它会把 **基础流的当前位置** 视为范围起点，只允许读取指定的最大长度。

### 使用示例（文件）

```csharp
using EasilyNET.Core.Essentials;

using var fileStream = new FileStream("input.bin", FileMode.Open, FileAccess.Read, FileShare.Read);

// 从当前位置开始，只允许读取接下来的 1MB
using var rangeStream = new RangeStream(fileStream, 1024 * 1024, leaveOpen: true);

var buffer = new byte[8192];
int read;
while ((read = rangeStream.Read(buffer, 0, buffer.Length)) > 0)
{
    // 处理 buffer[0..read]
}

// leaveOpen 为 true 时，rangeStream 释放后 fileStream 仍可继续使用
```

### 注意事项

- **范围起点**：构造时会记录基础流的当前位置，后续请避免在外部修改基础流的位置。
- **长度限制**：超过最大长度后读取将返回 `0`，表示范围结束。
- **Seek 行为**：仅在基础流支持 `CanSeek` 时可定位，且位置范围为 `0..Length`。
- **释放行为**：`leaveOpen=false`（默认）会在释放 `RangeStream` 时同时释放基础流。
