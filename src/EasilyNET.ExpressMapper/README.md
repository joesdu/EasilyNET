# EasilyNET.ExpressMapper

本项目从 [RosTransNadzor/ExpressMapper](https://github.com/RosTransNadzor/ExpressMapper) 复刻,原项目好像没有 Nuget 包,所以自己复制代码到这里,并且发布到 Nuget 上.添加了注释以及修改了部分拼写错误的代码.

# ExpressMapper

ExpressMapper 是一个轻量级且易于使用的对象到对象映射库,适用于 .NET,旨在简化在应用程序的各个层之间转换数据的过程.它提供了可自定义的映射配置、属性忽略、构造函数映射和复合配置,以轻松处理复杂的场景.

<details> 
<summary style="font-size: 14px">English</summary>

ExpressMapper is a lightweight and easy-to-use object-to-object mapping library for .NET, designed to streamline the process of transforming data between various layers of your application. It offers customizable mapping configurations, property ignoring, constructor mapping, and composite configurations to handle complex scenarios with ease.

</details>

## Features | 特性

- 可自定义的映射配置: 通过细粒度控制定义源属性和目标属性之间的映射.
- 忽略特定属性: 在映射过程中选择忽略源或目标端的属性.
- 构造函数映射: 在实例化目标对象时使用特定的构造函数,适用于复杂对象.
- 复合配置: 在单个配置类中管理不同类型的多个映射.

<details> 
<summary style="font-size: 14px">English</summary>

- **Customizable Mapping Configurations**: Define mappings between source and destination properties with fine-grained control.
- **Ignore Specific Properties**: Choose to ignore properties on either the source or destination side during the mapping.
- **Constructor Mapping**: Use specific constructors when instantiating destination objects, useful for complex objects.
- **Composite Configurations**: Manage multiple mappings for different types within a single configuration class.

</details>

## Installation | 安装

Install ExpressMapper via NuGet:

```sh
Install-Package EasilyNET.ExpressMapper
```

## Getting Started | 入门

要使用 ExpressMapper,首先定义源类和目标类,然后使用提供的配置类配置映射.

<details> 
<summary style="font-size: 14px">English</summary>

To use ExpressMapper, start by defining your source and destination classes, and then configure the mappings using the provided configuration classes.

</details>

### Example Classes | 示例类

```csharp
public class User
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class UserDto
{
    public string UserName { get; set; }
    public string AdditionalInfo { get; set; }
}
```

### Customizable Mapping Configurations | 可自定义的映射配置

定义不同类之间的属性映射方式:

<details> 
<summary style="font-size: 14px">English</summary>

Define how properties are mapped between different classes:

</details>

```csharp
file class UserConfig : MapperConfig<User, UserDto>
{
    public override void Configure(IMappingConfigurer<User, UserDto> configurer)
    {
        configurer
            .Map(dto => dto.UserName, us => us.Name);
    }
}
```

### Ignoring Properties | 忽略属性

在映射过程中排除某些属性:

<details> 
<summary style="font-size: 14px">English</summary>

Exclude certain properties from the mapping process:

</details>

```csharp
file class UserConfig : MapperConfig<User, UserDto>
{
    public override void Configure(IMappingConfigurer<User, UserDto> configurer)
    {
        configurer
            .Map(dto => dto.UserName, us => us.Name)
            .IgnoreDest(dto => dto.UserName)
            .IgnoreSource(us => us.Description);
    }
}
```

### Constructor Mapping | 构造函数映射

指定使用带有特定参数的构造函数:

<details> 
<summary style="font-size: 14px">English</summary>

Specify the use of constructors with specific parameters:

</details>

```csharp
configurer.WithConstructor<string, string>();
```

### Composite Configurations | 复合配置

将多个配置组合到一个配置类中:

<details> 
<summary style="font-size: 14px">English</summary>

Combine multiple configurations into a single configuration class:

</details>

```csharp
file class GeneralConfig : CompositeConfig
{
    public override void Configure()
    {
        NewConfiguration<Address, AddressDto>()
            .IgnoreDest(dto => dto.City);

        NewConfiguration<Product, ProductDto>()
            .Map(dto => dto.Id, product => product.ProductId);
    }

}

```

### Example Usage | 示例用法

```csharp
public static class Program
{
    static void Main(string[] args)
    {
        var mapper = Mapper.Create<GeneralConfig, UserConfig>();
        var user = new User
        {
            Description = "desc"
        };
        var dto = mapper.Map<User, UserDto>(user);
        Console.WriteLine(dto);
    }
}
```

这个示例演示了如何创建一个包含组合配置的映射器,并将 User 对象映射到 UserDto.

<details> 
<summary style="font-size: 14px">English</summary>

This example demonstrates how to create a mapper with combined configurations and map a `User` object to a `UserDto`.

</details>

## Conclusion | 结论

ExpressMapper 为 .NET 应用程序中的对象到对象映射提供了一个灵活且强大的解决方案.无论是简单的 DTO 还是复杂的对象,都可以根据您的具体需求自定义映射.

<details> 
<summary style="font-size: 14px">English</summary>

ExpressMapper provides a flexible and powerful solution for object-to-object mapping in .NET applications. Customize your mappings to fit your specific needs, whether it's for simple DTOs or complex objects.

For more information, please refer to the official documentation or check out the source code examples.

</details>
