##### EasilyNET.Core.Domain.SourceGenerator
##### Nuget

使用 Nuget 包管理工具添加依赖包 
EasilyNET.Core.Domains
EasilyNET.Core.Domain.SourceGenerator

##### 核心
接口IHasCreationTime，IHasModifierId，IMayHaveCreator，IHasDeleterId，IHasDeletionTime，IHasModificationTime

如使用以上接口请自行选择

****必须是`public`，`partial` 类才会自动生成代码
```csharp
public partial class Test : Entity<long>, IHasCreationTime, IHasModifierId<long?>, IMayHaveCreator<long?>, IHasDeleterId<long?>, IHasDeletionTime, IHasModificationTime;
```



