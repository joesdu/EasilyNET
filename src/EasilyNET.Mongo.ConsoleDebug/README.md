### EasilyNET.Mongo.ConsoleDebug

> 常使用 EF 的小伙伴就应该能够知道,可以让 EF 生产的 SQL 语句输出到控制台,在开发的时候非常方便调试.<br/>
> 而 MongoDB 却没有这样的功能,所以产生了这个库,虽然不完美,但是能够解决一些开发过程中不方便排查问题的情况.

- 最终效果类似如下:

```text
 ╭─────────────────────────────────────────Command Json─────────────────────────────────────────╮╭──────────────────Calendar──────────────────╮
 │ {                                                                                            ││                2023 August                 │
 │   "insert" : "mongo.test",                                                                   ││ ┌─────┬─────┬─────┬─────┬─────┬─────┬────┐ │
 │   "ordered" : true,                                                                          ││ │ Sun │ Mon │ Tue │ Wed │ Thu │ Fri │ S… │ │
 │   "$db" : "test1",                                                                           ││ ├─────┼─────┼─────┼─────┼─────┼─────┼────┤ │
 │   "lsid" : {                                                                                 ││ │     │     │ 1   │ 2   │ 3   │ 4   │ 5  │ │
 │     "id" : CSUUID("163f8243-956c-43e3-8b19-e2f29dffe5ea")                                    ││ │ 6   │ 7   │ 8   │ 9   │ 10  │ 11  │ 12 │ │
 │   },                                                                                         ││ │ 13  │ 14  │ 15  │ 16  │ 17  │ 18* │ 19 │ │
 │   "documents" : [{                                                                           ││ │ 20  │ 21  │ 22  │ 23  │ 24  │ 25  │ 26 │ │
 │       "_id" : ObjectId("64df334c7f6a98097602b4c6"),                                          ││ │ 27  │ 28  │ 29  │ 30  │ 31  │     │    │ │
 │       "dateTime" : ISODate("2023-08-18T09:01:00.861Z"),                                      ││ │     │     │     │     │     │     │    │ │
 │       "timeSpan" : "00:00:50",                                                               ││ └─────┴─────┴─────┴─────┴─────┴─────┴────┘ │
 │       "dateOnly" : "2023-08-18",                                                             │╰────────────────────────────────────────────╯
 │       "timeOnly" : "17:01:00"                                                                │╭─────────────────Mongo Info─────────────────╮
 │     }]                                                                                       ││ {                                          │
 │ }                                                                                            ││    "RequestId": 71,                        │
 │                                                                                              ││    "Timestamp": "2023-08-18 09:01:00",     │
 │                                                                                              ││    "Method": "insert",                     │
 │                                                                                              ││    "DatabaseName": "test1",                │
 │                                                                                              ││    "CollectionName": "mongo.test",         │
 │                                                                                              ││    "ConnectionInfo": {                     │
 │                                                                                              ││       "ClusterId": 1,                      │
 │                                                                                              ││       "EndPoint": "127.0.0.1:27018"        │
 │                                                                                              ││    }                                       │
 │                                                                                              ││ }                                          │
 │                                                                                              │╰────────────────────────────────────────────╯
 │                                                                                              │╭────────────Mongo Request Status────────────╮
 │                                                                                              ││ ┌───────────┬────────────────┬───────────┐ │
 │                                                                                              ││ │ RequestId │      Time      │  Status   │ │
 │                                                                                              ││ ├───────────┼────────────────┼───────────┤ │
 │                                                                                              ││ │    71     │ 17:01:00.96285 │ Succeeded │ │
 │                                                                                              ││ └───────────┴────────────────┴───────────┘ │
 │                                                                                              │╰────────────────────────────────────────────╯
 │                                                                                              │╭───────────────YongGan NiuNiu───────────────╮
 │                                                                                              ││  ________________________________________  │
 │                                                                                              ││ /     Only two things are infinite,      \ │
 │                                                                                              ││ \   the universe and human stupidity.    / │
 │                                                                                              ││  ----------------------------------------  │
 │                                                                                              ││              ^__^     O   ^__^             │
 │                                                                                              ││      _______/(oo)      o  (oo)\_______     │
 │                                                                                              ││  /\/(       /(__)         (__)\       )\/\ │
 │                                                                                              ││     ||w----||                 ||----w||    │
 │                                                                                              ││     ||     ||                 ||     ||    │
 │                                                                                              ││ ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ │
 ╰──────────────────────────────────────────────────────────────────────────────────────────────╯╰────────────────────────────────────────────╯
```

### 使用方法

- 使用默认值配置

```csharp
var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventSubscriber());
var mongoClient = new MongoClient(clientSettings);
```

- 使用集合名称进行过滤

```csharp
var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
var options = new InstrumentationOptions { ShouldStartActivity = @event => !"collectionToIgnore".Equals(@event.GetCollectionName()) };
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber(options));
var mongoClient = new MongoClient(clientSettings);
```

- 该库参考 [SkyAPM-dotnet MongoDB](https://github.com/SkyAPM/SkyAPM-dotnet)
- 推荐和 [Serilog.Sinks.Spectre](https://github.com/lucadecamillis/serilog-sinks-spectre) 一起使用效果最佳

###### Seilog配置例子

```csharp
// 添加Serilog配置
builder.Host.UseSerilog((hbc, lc) =>
{
    const LogEventLevel logLevel = LogEventLevel.Information;
    lc.ReadFrom.Configuration(hbc.Configuration)
          .MinimumLevel.Override("Microsoft", logLevel)
          .MinimumLevel.Override("System", logLevel)
          .Enrich.FromLogContext()
          .WriteTo.Async(wt =>
          {
              wt.Debug();
              // 输出到 Spectre.Console
              wt.Spectre();
          });
});
```


同时参考[MongoDB.Driver.Core.Extensions.DiagnosticSources](https://github.com/jbogard/MongoDB.Driver.Core.Extensions.DiagnosticSources)
