using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Configuration.Config.Clause;

/// <summary>
/// Implementation of the map clause.
/// 映射子句的实现。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
/// <typeparam name="TMember">The member type. 成员类型。</typeparam>
public class MapClause<TSource, TDest, TMember> : IMapClause<TSource, TDest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapClause{TSource, TDest, TMember}" /> class.
    /// 初始化 <see cref="MapClause{TSource, TDest, TMember}" /> 类的新实例。
    /// </summary>
    /// <param name="destMemberAccess">The destination member access expression. 目标成员访问表达式。</param>
    /// <param name="lambdaExpression">The lambda expression. lambda 表达式。</param>
    public MapClause(Expression<Func<TDest, TMember>> destMemberAccess, Expression<Func<TSource, TMember>> lambdaExpression)
    {
        if (!DefineIfValidMemberAccess(destMemberAccess)) return;
        var member = (MemberExpression)destMemberAccess.Body;
        var name = member.Member.Name;
        var type = typeof(TMember);
        DestinationMember = new()
        {
            Info = member.Member,
            Type = type,
            Name = name
        };
        Expression = lambdaExpression;
        IsValidClause = true;
    }

    /// <summary>
    /// Gets a value indicating whether this clause is valid.
    /// 获取一个值，该值指示此子句是否有效。
    /// </summary>
    public bool IsValidClause { get; }

    /// <summary>
    /// Gets the destination member.
    /// 获取目标成员。
    /// </summary>
    public MappingMember? DestinationMember { get; }

    /// <summary>
    /// Gets the lambda expression.
    /// 获取 lambda 表达式。
    /// </summary>
    public LambdaExpression? Expression { get; }

    /// <summary>
    /// Defines if the member access is valid based on the destination member access expression.
    /// 根据目标成员访问表达式定义成员访问是否有效。
    /// </summary>
    /// <param name="destMemberAccess">The destination member access expression. 目标成员访问表达式。</param>
    /// <returns>True if the member access is valid; otherwise, false. 如果成员访问有效，则为 true；否则为 false。</returns>
    private static bool DefineIfValidMemberAccess(Expression<Func<TDest, TMember>> destMemberAccess) => destMemberAccess.Body is MemberExpression;
}