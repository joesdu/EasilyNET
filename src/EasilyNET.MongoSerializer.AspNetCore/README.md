#### EasilyNET.MongoSerializer.AspNetCore

EasilyNET.Mongo.AspNetCore 扩展,用于支持一些非默认类型的序列化方案.就算是没有使用 EasilyNET.Mongo.AspNetCore 也能使用这个库进行如下内容的扩展.

##### ChangeLogs

- 支持自定义 TimeOnly 和 DateOnly 的格式化格式.
  1. 支持转换成字符串格式
  2. 转换成 Ticks 的方式存储
  3. 若想转化成其他类型也可自行实现,如:转化成 ulong 类型
- 添加动态类型支持[object 和 dynamic]
- 添加.Net6 Date/Time Only 类型支持(TimeOnly 理论上应该是兼容原 TimeSpan 数据类型).

---

- 配合 EasilyNET.Mongo.AspNetCore 或单独使用

```csharp
builder.Services.AddMongoContext<DbContext>(builder.Configuration)
// 添加自定义序列化
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());
// 或者将他们存储为long类型的Ticks,也可以自己组合使用.
builder.Services.RegisterSerializer(new DateOnlySerializerAsTicks());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsTicks());

// 添加动态类型(dynamic|object)支持
builder.Services.RegisterDynamicSerializer();
```
