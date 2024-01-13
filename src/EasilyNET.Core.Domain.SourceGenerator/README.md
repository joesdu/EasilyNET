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
****使用生成器，生成好代码如下：
1.`virtual` 属性，支持自己重写
2.`set`访问属性为`public`,后面会支持其他属性。
```csharp
public virtual DateTime CreationTime { get; set; }
public virtual long? CreatorId { get; set; }
public virtual long? LastModifierId { get; set; }
public virtual long? DeleterId { get; set; }
public virtual DateTime? DeletionTime { get; set; }
public virtual DateTime? LastModificationTime { get; set; }
```






