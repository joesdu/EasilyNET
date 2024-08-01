#### 用于 EasilyNET 的 SourceGenerator

**注意:**由于源码生成器的特征,需要使用源码生成器的项目需要显示引入该库,间接引入将无法正常工作,并且需要显示标记为`OutputItemType="Analyzer"`

```xml
	<PackageReference Include="EasilyNET.Core.SourceGenerator" Version="1.0.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
```

- 用于枚举类型的源代码生成器,`EnumDescriptionGenerator` 用于生成枚举类型的描述信息

```csharp
	public enum EnumType
	{
		[Description("枚举项描述")]
		EnumItem
	}

	// 生成后,使用

	EnumType.EnumItem.ToDescription(); // 枚举项描述
```
