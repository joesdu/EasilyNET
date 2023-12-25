namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 模型绑定扩展
/// </summary>
public static class ModelBuilderExtensions
{
    private static readonly MethodInfo _methodInfo = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));

    /// <summary>
    /// 设置软删除字段
    /// </summary>
    /// <param name="builder"></param>
    public static void AddIsDeletedField(this ModelBuilder builder)
    {
        var types = builder.Model.GetEntityTypes().Where(o => typeof(IHasSoftDelete).IsAssignableFrom(o.ClrType)).ToList();
        foreach (var type in types)
        {
            builder.Entity(type.ClrType).Property<bool>(EFCoreShare.IsDeleted);
            builder.Entity(type.ClrType).HasQueryFilter(GetDeleteLambda(type.ClrType));
        }
    }

    /// <summary>
    /// 获取过滤条件
    /// </summary>
    /// <param name="clrType"></param>
    /// <returns></returns>
    public static LambdaExpression GetDeleteLambda(Type clrType)
    {
        var param = Expression.Parameter(clrType, "it");

        //EF.Property<bool>(it, "IsDeleted")
        Expression call = Expression.Call(_methodInfo, param, Expression.Constant(EFCoreShare.IsDeleted));

        //(EF.Property<bool>(it, "IsDeleted") == False)
        var binaryExpression = Expression.MakeBinary(ExpressionType.Equal, call, Expression.Constant(false, typeof(bool)));

        // it => EF.Property<bool>(it, "Deleted") == False
        var lambda = Expression.Lambda(binaryExpression, param);
        return lambda;
    }
}