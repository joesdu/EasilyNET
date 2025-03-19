[^ **\_\_**Back**\_\_** ^](..\README.md)

> 为 C# 添加语法糖,使得可以使用 `..` 来代替 `Enumerable.Range` 方法,以及 `..` 语法糖的扩展方法.

<details>
<summary style="font-size: 14px">English</summary>

> Add syntactic sugar to C#, so that you can use `..` instead of the `Enumerable.Range` method, as well as the extension
> method of the `..` syntactic sugar.

</details>

### **使用方法(Usage)**

```csharp
// 生成 1 到 10 的序列
var range = 1..10;
```

---

```csharp
foreach (var i in ..3)
{
    Console.WriteLine(i);
}
// Output: 0, 1, 2, 3
```

---

```csharp
foreach (var i in 1..3)
{
    Console.WriteLine(i);
}
// Output: 1, 2, 3
```

---

```csharp
foreach (var i in 3)
{
    Console.WriteLine(i);
}
// Output: 0, 1, 2, 3
```
