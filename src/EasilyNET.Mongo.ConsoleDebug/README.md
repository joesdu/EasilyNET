### EasilyNET.Mongo.ConsoleDebug

> 常使用 EF 的小伙伴就应该能够知道,可以让 EF 生产的 SQL 语句输出到控制台,在开发的时候非常方便调试.而 MongoDB
> 却没有这样的功能,所以产生了这个库,虽然不完美,但是能够解决一些开发过程中不方便排查问题的情况.

- 最终效果类似如下:

```text
[16:05:26 INF] MongoRequest: 12,Command:
{
  "find" : "mongo.test2",
  "filter" : {
    "_id" : "c7c5d0f8-b57d-4901-913d-8a5cfacf1286"
  },
  "limit" : 2,
  "$db" : "hoyo",
  "lsid" : {
    "id" : CSUUID("498de1a4-a352-40f2-9634-d49627f609aa")
  }
}
[16:05:26 INF] MongoRequest: 12,Status: Succeeded
```

- 对命令文本进行简要的分析,因为不同的命令会产生不同的结构.

  | 名称   | 含义                                          |
        | ------ | --------------------------------------------- |
  | find   | 表示该命令为查询命令,他的值就是查询的集合名称 |
  | filter | 表示查询条件                                  |
  | limit  | 表示查询的数据量                              |
  | \$db   | 表示执行该命令的数据库                        |

- 可以看到命令文本前加了 MongoRequest 表示请求 ID,同时后边显示了该请求的成功状态. Succeeded 表示执行成功,Failed 表示执行失败.

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

- 该库参考[SkyAPM-dotnet MongoDB](https://github.com/SkyAPM/SkyAPM-dotnet)
-

同时参考[MongoDB.Driver.Core.Extensions.DiagnosticSources](https://github.com/jbogard/MongoDB.Driver.Core.Extensions.DiagnosticSources)
