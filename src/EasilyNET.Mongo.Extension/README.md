#### EasilyNET.Mongo.Extension

EasilyNET.Mongo扩展,用于支持一些非默认类型的序列化方案.

##### ChangeLogs

- 添加动态类型支持
- 添加.Net6 Date/Time Only 类型支持(TimeOnly 理论上应该是兼容原 TimeSpan 数据类型).

---
- 配合 EasilyNET.Mongo 使用

```csharp
builder.Services.AddMongoContext<DbContext>(builder.Configuration).RegisterSerializer();
// 添加动态类型(dynamic|object)支持
builder.Services.RegisterDynamicSerializer();
```
