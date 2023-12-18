#### EasilyNET.AutoInjection.SourceGenerator

## Install Package

```shell
Install-Package EasilyNET.AutoInjection.SourceGenerator
```

## 创建要自动注入

  ### 使用特性注入
  ```csharp
[DependencyInjection(ServiceLifetime.Scoped)]
public class Test13
{
    public void GetTest()
    {
        Console.WriteLine($"{nameof(Test13)}");
    }
}
```

  ### 使用接口
  ```csharp
public interface ITestTransient : ITransientDependency
{

}

public class TestTransient : ITestTransient
 {

 }
```

## 添加自动注入
 1.builder.Services.AddAutoInjection();
 2.添加本程序引用（using EasilyNET.AutoInjection.SourceGenerator.Console.Test）

  ### 如何自定义名字
   AddAutoXXX1()
   把开当前项目下.csproj文件
   添加如下内容：
   ```shell
     	<PropertyGroup>
		<InjectionName>自定义名字</InjectionName>
	</PropertyGroup>
	<ItemGroup>
		<CompilerVisibleProperty Include="InjectionName" /> 
	</ItemGroup>
   ```
   ### use
   AddAuto自定义名字();



