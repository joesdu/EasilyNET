using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Update;

namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <summary>
/// 迁移文件不生成外键关系和外键索引
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage.
public class MigrationsModelDifferWithoutForeignKey : MigrationsModelDiffer
{
    // 判断 .NET 6
#if NET6_0
    /// <summary>
    /// </summary>
    /// <param name="typeMappingSource"></param>
    /// <param name="migrationsAnnotations"></param>
    /// <param name="changeDetector"></param>
    /// <param name="updateAdapterFactory"></param>
    /// <param name="commandBatchPreparerDependencies"></param>
    public MigrationsModelDifferWithoutForeignKey(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotations,
#pragma warning disable EF1001
        IChangeDetector
            changeDetector,
#pragma warning restore EF1001 // Internal EF Core API usage.
#pragma warning disable EF1001 // Internal EF Core API usage.
        IUpdateAdapterFactory
            updateAdapterFactory,
#pragma warning restore EF1001 // Internal EF Core API usag
#pragma warning disable EF1001 // Internal EF Core API usage.
        CommandBatchPreparerDependencies
            commandBatchPreparerDependencies
#pragma warning restore EF1001 // Internal EF Core API usage.
    )
        : base(typeMappingSource, migrationsAnnotations,
            changeDetector,
            updateAdapterFactory, commandBatchPreparerDependencies)
    { }
#endif

#if NET7_0_OR_GREATER
     /// <summary>
    /// 
    /// </summary>
    /// <param name="typeMappingSource"></param>
    /// <param name="migrationsAnnotationProvider"></param>
    /// <param name="rowIdentityMapFactory"></param>
    /// <param name="commandBatchPreparerDependencies"></param>
    public MigrationsModelDifferWithoutForeignKey(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRowIdentityMapFactory 
        rowIdentityMapFactory,
        CommandBatchPreparerDependencies
            commandBatchPreparerDependencies
    ) : base(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory,
        commandBatchPreparerDependencies)
    { }
#endif

    /// <summary>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
    {
#pragma warning disable EF1001 // Internal EF Core API usage.
        var getDifferences = base.GetDifferences(source, target);
#pragma warning restore EF1001 // Internal EF Core API usage.
        var operations = getDifferences
                         .Where(op => op is not AddForeignKeyOperation)
                         .Where(op => op is not DropForeignKeyOperation)
                         .ToList();
        List<AddForeignKeyOperation> foreignKeyOperations = new();
        foreach (var operation in operations.OfType<CreateTableOperation>())
        {
            foreignKeyOperations.AddRange(operation.ForeignKeys.ToArray());
            operation.ForeignKeys.Clear();
        }
        var foreignKes = foreignKeyOperations.Select(o => $"IX_{o.Table}_{string.Join("_", o.Columns)}");
        List<MigrationOperation> list = new();
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
#pragma warning restore EF1001 // Internal EF Core API usage.