#### EasilyNET.Mongo.Extension

EasilyNET.Mongo扩展,用于支持一些非默认类型的序列化方案.就算是没有使用 EasilyNET.Mongo 也能使用这个库进行如下内容的扩展.

##### ChangeLogs

- 支持自定义TimeOnly和DateOnly的格式化格式.(仅支持字符串方式).若想转化成其他类型请自行实现,如:转化成ulong类型
- 添加动态类型支持
- 添加.Net6 Date/Time Only 类型支持(TimeOnly 理论上应该是兼容原 TimeSpan 数据类型).

---
- 配合 EasilyNET.Mongo 或单独使用

```csharp
builder.Services.AddMongoContext<DbContext>(builder.Configuration).RegisterSerializer();
// 添加动态类型(dynamic|object)支持
builder.Services.RegisterDynamicSerializer();
```
