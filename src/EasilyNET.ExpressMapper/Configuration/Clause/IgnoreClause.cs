using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Configuration.Config.Clause;

/// <summary>
/// Implementation of the ignore clause.
/// 忽略子句的实现。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
/// <typeparam name="TMember">The member type. 成员类型。</typeparam>
public class IgnoreClause<TSource, TDest, TMember> : IIgnoreClause<TSource, TDest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreClause{TSource, TDest, TMember}" /> class with a source expression.
    /// 使用源表达式初始化 <see cref="IgnoreClause{TSource, TDest, TMember}" /> 类的新实例。
    /// </summary>
    /// <param name="sourceExpression">The source expression. 源表达式。</param>
    public IgnoreClause(Expression<Func<TSource, TMember>> sourceExpression)
    {
        if (!DefineIfValidClause(sourceExpression)) return;
        var member = (MemberExpression)sourceExpression.Body;
        var name = member.Member.Name;
        var type = typeof(TMember);
        SourceIgnoreMember = new()
        {
            Info = member.Member,
            Type = type,
            Name = name
        };
        IsValidClause = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreClause{TSource, TDest, TMember}" /> class with a destination expression.
    /// 使用目标表达式初始化 <see cref="IgnoreClause{TSource, TDest, TMember}" /> 类的新实例。
    /// </summary>
    /// <param name="destinationExpression">The destination expression. 目标表达式。</param>
    public IgnoreClause(Expression<Func<TDest, TMember>> destinationExpression)
    {
        if (!DefineIfValidClause(destinationExpression)) return;
        var member = (MemberExpression)destinationExpression.Body;
        var name = member.Member.Name;
        var type = typeof(TMember);
        DestinationIgnoreMember = new()
        {
            Info = member.Member,
            Type = type,
            Name = name
        };
        IsValidClause = true;
    }

    /// <summary>
    /// Gets the source ignore member.
    /// 获取源忽略成员。
    /// </summary>
    public MappingMember? SourceIgnoreMember { get; }

    /// <summary>
    /// Gets the destination ignore member.
    /// 获取目标忽略成员。
    /// </summary>
    public MappingMember? DestinationIgnoreMember { get; }

    /// <summary>
    /// Gets a value indicating whether this clause is valid.
    /// 获取一个值，该值指示此子句是否有效。
    /// </summary>
    public bool IsValidClause { get; }

    /// <summary>
    /// Defines if the clause is valid based on the destination expression.
    /// 根据目标表达式定义子句是否有效。
    /// </summary>
    /// <param name="dest">The destination expression. 目标表达式。</param>
    /// <returns>True if the clause is valid; otherwise, false. 如果子句有效，则为 true；否则为 false。</returns>
    private static bool DefineIfValidClause(Expression<Func<TDest, TMember>> dest) => dest.Body.NodeType is ExpressionType.MemberAccess;

    /// <summary>
    /// Defines if the clause is valid based on the source expression.
    /// 根据源表达式定义子句是否有效。
    /// </summary>
    /// <param name="dest">The source expression. 源表达式。</param>
    /// <returns>True if the clause is valid; otherwise, false. 如果子句有效，则为 true；否则为 false。</returns>
    private static bool DefineIfValidClause(Expression<Func<TSource, TMember>> dest) => dest.Body.NodeType is ExpressionType.MemberAccess;
}