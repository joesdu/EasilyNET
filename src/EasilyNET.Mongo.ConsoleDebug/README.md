### EasilyNET.Mongo.ConsoleDebug

> 常使用 EF 的小伙伴就应该能够知道,可以让 EF 生产的 SQL 语句输出到控制台,在开发的时候非常方便调试.<br/>
> 而 MongoDB 却没有这样的功能,所以产生了这个库,虽然不完美,但是能够解决一些开发过程中不方便排查问题的情况.

- 最终效果类似如下:

```text
 ╭───────────────────────────────Command─────────────────────────╮╭──────────────────Calendar──────────────────╮
 │ {                                                             ││                2023 August                 │
 │   "insert" : "mongo.test",                                    ││ ┌─────┬─────┬─────┬─────┬─────┬─────┬────┐ │
 │   "ordered" : true,                                           ││ │ Sun │ Mon │ Tue │ Wed │ Thu │ Fri │ S… │ │
 │   "$db" : "test1",                                            ││ ├─────┼─────┼─────┼─────┼─────┼─────┼────┤ │
 │   "lsid" : {                                                  ││ │     │     │ 1   │ 2   │ 3   │ 4   │ 5  │ │
 │     "id" : CSUUID("f12dd90d-2f58-4655-9bf2-cbce2d9bd2c4")     ││ │ 6   │ 7   │ 8   │ 9   │ 10  │ 11  │ 12 │ │
 │   },                                                          ││ │ 13  │ 14  │ 15  │ 16  │ 17  │ 18  │ 19 │ │
 │   "documents" : [{                                            ││ │ 20  │ 21  │ 22  │ 23* │ 24  │ 25  │ 26 │ │
 │       "_id" : ObjectId("64e57f266a1a63e69c52b9cb"),           ││ │ 27  │ 28  │ 29  │ 30  │ 31  │     │    │ │
 │       "dateTime" : ISODate("2023-08-23T03:38:14.121Z"),       ││ │     │     │     │     │     │     │    │ │
 │       "timeSpan" : "00:00:50",                                ││ └─────┴─────┴─────┴─────┴─────┴─────┴────┘ │
 │       "dateOnly" : "2023-08-23",                              │╰────────────────────────────────────────────╯
 │       "timeOnly" : "11:38:14",                                │╭────────────────────Info────────────────────╮
 │       "nullableDateOnly" : "2023-08-23",                      ││ {                                          │
 │       "nullableTimeOnly" : null                               ││    "RequestId": 86,                        │
 │     }]                                                        ││    "Timestamp": "2023-08-23 03:38:14",     │
 │ }                                                             ││    "Method": "insert",                     │
 │                                                               ││    "DatabaseName": "test1",                │
 │                                                               ││    "CollectionName": "mongo.test",         │
 │                                                               ││    "ConnectionInfo": {                     │
 │                                                               ││       "ClusterId": 1,                      │
 │                                                               ││       "EndPoint": "127.0.0.1:27018"        │
 │                                                               ││    }                                       │
 │                                                               ││ }                                          │
 │                                                               │╰────────────────────────────────────────────╯
 │                                                               │╭───────────────Request Status───────────────╮
 │                                                               ││ ┌───────────┬────────────────┬───────────┐ │
 │                                                               ││ │ RequestId │      Time      │  Status   │ │
 │                                                               ││ ├───────────┼────────────────┼───────────┤ │
 │                                                               ││ │    86     │ 11:38:14.12640 │ Succeeded │ │
 │                                                               ││ └───────────┴────────────────┴───────────┘ │
 │                                                               │╰────────────────────────────────────────────╯
 │                                                               │╭───────────────────NiuNiu───────────────────╮
 │                                                               ││   --------------------------------------   │
 │                                                               ││ /     Only two things are infinite,      \ │
 │                                                               ││ \   the universe and human stupidity.    / │
 │                                                               ││   --------------------------------------   │
 │                                                               ││              ^__^     O   ^__^             │
 │                                                               ││      _______/(oo)      o  (oo)\_______     │
 │                                                               ││  /\/(       /(__)         (__)\       )\/\ │
 │                                                               ││     ||w----||                 ||----w||    │
 │                                                               ││     ||     ||                 ||     ||    │
 │                                                               ││ ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ │
 ╰───────────────────────────────────────────────────────────────╯╰────────────────────────────────────────────╯
```

### 使用方法

- 使用默认值配置

```csharp
var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventConsoleDebugSubscriber());
var mongoClient = new MongoClient(clientSettings);
```

- 使用集合名称进行过滤

```csharp
var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
// 定义需要输出的集合
HashSet<string> CommandsWithCollectionName = new()
{
    "mongo.test"
};
var options = new InstrumentationOptions()
{
    Enable = true,
    ShouldStartCollection = coll => CommandsWithCollectionName.Contains(coll)
};
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventConsoleDebugSubscriber(options));
var mongoClient = new MongoClient(clientSettings);
```

- 添加 MongoDB 诊断信息输出到 OpenTelemetry
```csharp
// 在上面的基础上,添加如下代码
clientSettings.ClusterConfigurator = cb =>
{
    s.Subscribe(new ActivityEventConsoleDebugSubscriber(new()
    {
        Enable = true
    }));
    s.Subscribe(new ActivityEventDiagnosticsSubscriber(new()
    {
        CaptureCommandText = true
    }));
};}

```

同时参考[MongoDB.Driver.Core.Extensions.DiagnosticSources](https://github.com/jbogard/MongoDB.Driver.Core.Extensions.DiagnosticSources)
