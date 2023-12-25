namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <remarks>
/// 删除外键迁移Sql生成
/// </remarks>
/// <param name="dependencies"></param>
public class RemoveForeignKeyMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies) : MigrationsSqlGenerator(dependencies)
{
    /// <summary>
    /// 外键操作集合
    /// </summary>
    private readonly IList<AddForeignKeyOperation> _addForeignKeyOperations = new List<AddForeignKeyOperation>();

    /// <summary>
    /// 创建表操作
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="model"></param>
    /// <param name="builder"></param>
    /// <param name="terminate"></param>
    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        base.Generate(operation, model, builder, terminate);
    }

    /// <summary>
    /// 创建表外键
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="model"></param>
    /// <param name="builder"></param>
    protected override void CreateTableForeignKeys(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        //把外键添加
        _addForeignKeyOperations.AddRange(operation.ForeignKeys);
        operation.ForeignKeys.Clear();
        base.CreateTableForeignKeys(operation, model, builder);
    }

    /// <summary>
    /// 创建索引
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="model"></param>
    /// <param name="builder"></param>
    /// <param name="terminate"></param>
    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        //是否外键索引,如果是就不创建，否则就创建
        var isForeignKeyIndex = _addForeignKeyOperations?.FirstOrDefault(o => $"IX_{o.Table}_{string.Join("_", o.Columns)}" == operation.Name);
        if (isForeignKeyIndex is null)
        {
            base.Generate(operation, model, builder, terminate);
        }
    }
}