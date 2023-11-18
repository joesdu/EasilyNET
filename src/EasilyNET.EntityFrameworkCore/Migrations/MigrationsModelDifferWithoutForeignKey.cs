#if NET7_0_OR_GREATER
#pragma warning disable EF1001 // Internal EF Core API usage.
#endif
#if NET6_0
#pragma warning disable EF1001 // Internal EF Core API usage.
#endif
namespace EasilyNET.EntityFrameworkCore.Migrations;

#if NET6_0
/// <summary>
/// 迁移文件不生成外键关系和外键索引
/// </summary>
/// <param name="mappingSource"></param>
/// <param name="provider"></param>
/// <param name="detector"></param>
/// <param name="factory"></param>
/// <param name="dependencies"></param>
public class MigrationsModelDifferWithoutForeignKey(IRelationalTypeMappingSource mappingSource, IMigrationsAnnotationProvider provider, IChangeDetector detector, IUpdateAdapterFactory factory, CommandBatchPreparerDependencies dependencies)
    : MigrationsModelDiffer(mappingSource, provider, detector, factory, dependencies)
#elif NET7_0_OR_GREATER
/// <summary>
/// 迁移文件不生成外键关系和外键索引
/// </summary>
/// <param name="mappingSource"></param>
/// <param name="provider"></param>
/// <param name="factory"></param>
/// <param name="dependencies"></param>
public class MigrationsModelDifferWithoutForeignKey(IRelationalTypeMappingSource mappingSource, IMigrationsAnnotationProvider provider, IRowIdentityMapFactory factory, CommandBatchPreparerDependencies dependencies)
    : MigrationsModelDiffer(mappingSource, provider, factory, dependencies)
#endif
{
    /// <summary>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
    {
        var getDifferences = base.GetDifferences(source, target);
        var operations = getDifferences
                         .Where(op => op is not AddForeignKeyOperation)
                         .Where(op => op is not DropForeignKeyOperation)
                         .ToList();
        List<AddForeignKeyOperation> foreignKeyOperations = [];
        foreach (var operation in operations.OfType<CreateTableOperation>())
        {
            foreignKeyOperations.AddRange(operation.ForeignKeys.ToArray());
            operation.ForeignKeys.Clear();
        }
        var foreignKes = foreignKeyOperations.Select(o => $"IX_{o.Table}_{string.Join("_", o.Columns)}");
        List<MigrationOperation> list = [];
        foreach (var difference in getDifferences)
        {
            if (!(difference is CreateIndexOperation result && foreignKes.Any(o => o == result.Name)))
            {
                list.Add(difference);
            }
        }
        return list;
    }
}