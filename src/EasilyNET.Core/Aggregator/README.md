[^ **\_\_**Back**\_\_** ^](..\README.md)

#### SimpleEventAggregator

> 简单的事件聚合器，用于解耦事件发布者和订阅者。在事件发布者和订阅者之间建立一对多的关系，让订阅者订阅事件，当事件发布者发布事件时，订阅者可以接收到事件。

<details>
<summary style="font-size: 14px">English</summary>

> A simple event aggregator that decouples event publishers and subscribers. Establish a one-to-many relationship
> between event publishers and subscribers, allowing subscribers to subscribe to events, and when event publishers
> publish
> events, subscribers can receive events.

</details>

##### 使用方法(Usage)

- 这里采用依赖注入的模式来提供示例代码,默认认为你已经了解依赖注入的基本概念,并且注入了 SimpleEventAggregator 实例.
- 其他方式请手动管理 SimpleEventAggregator 实例的生命周期.推荐使用单例模式.
- 以下是在依赖注入模式下使用 SimpleEventAggregator 的示例代码.

<details>
<summary style="font-size: 14px">English</summary>

- Here we use the dependency injection pattern to provide sample code, assuming you already understand the basic
  concepts of dependency injection and have injected the SimpleEventAggregator instance.
- Other ways to manually manage the lifecycle of the SimpleEventAggregator instance. It is recommended to use the
  singleton pattern.
- The following is an example of using the SimpleEventAggregator in the dependency injection pattern.

</details>

```csharp
// Register Service
builder.Services.AddSingleton<IEventAggregator, SimpleEventAggregator>();
```

```csharp
// 定义事件
public class MyEvent : EventArgs
{
    public string Message { get; set; }
}

// 在类A中订阅事件
public class MyClassA
{
    public MyClassA(IEventAggregator aggregator)
    {
        aggregator.Subscribe<MyEvent>(MySubscriberHandler);
    }

    private void MySubscriberHandler(MyEvent e)
    {
        Console.WriteLine($"MyClassA receive message:{e.Message}");
    }
}

// 在类B中发布事件
public class MyClassB(IEventAggregator aggregator)
{
    public void PublishEvent()
    {
        aggregator.Publish<MyEvent>(new() { Message = "Hello,World!" });
    }
}
```
