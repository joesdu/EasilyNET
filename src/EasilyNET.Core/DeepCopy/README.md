[^ **\_\_**Back**\_\_** ^](..\README.md)

#### DeepCopy

> 基于表达式树和 Reflection 的 DeepCopy(推荐使用表达式树的版本,性能更好),支持深拷贝.

<details>
<summary style="font-size: 14px">English</summary>

> DeepCopy based on expression tree and Reflection (it is recommended to use the version of expression tree, which has better performance), supporting deep copy.

</details>

##### 使用方法(Usage)

```csharp
class Person
{
    public string Name { get; set; } = "Foo";
}

var person= new Person();
var person2 = person.DeepCopy();
```
