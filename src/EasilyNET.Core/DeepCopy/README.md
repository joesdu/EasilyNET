[^ **\_\_**Back**\_\_** ^](..\README.md)

#### DeepCopy

> 基于表达式树的 DeepCopy,支持深拷贝.

<details>
<summary style="font-size: 14px">English</summary>

> DeepCopy based on expression tree, supporting deep copy.

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
