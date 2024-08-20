namespace EasilyNET.Test.Unit.ExpressMapper.Entities;
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

public class Product
{
    public string ProductName { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class ProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; }
}