namespace EasilyNET.Test.Unit.ExpressMapper.Entities;
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

public class Address
{
    public string City { get; set; }

    public string Country { get; set; }

    public string Street { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class AddressDto(string city, string country, string street)
{
    public string City { get; private set; } = city;

    public string Country { get; private set; } = country;

    public string Street { get; private set; } = street;
}