#### EasilyNET.Mongo.Extension

- 添加.Net6 Date/Time Only 类型支持(TimeOnly 理论上应该是兼容原 TimeSpan 数据类型).

- 配合 EasilyNET.Mongo 使用

```csharp
builder.Services.AddMongoContext<DbContext>(builder.Configuration).RegisterEasilyNETSerializer();
```
