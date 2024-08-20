namespace EasilyNET.Test.Unit.ExpressMapper.Entities;
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

public class User
{
    public int Age { get; init; }

    public string Name { get; set; }

    public string Description { get; init; }

    public string AnotherDescription { get; init; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class UserDto
{
    // ReSharper disable once UnassignedReadonlyField
    public readonly string AnotherDescription;

    public int Age { get; init; }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public string Name { get; }

    public string Description { get; init; }
}