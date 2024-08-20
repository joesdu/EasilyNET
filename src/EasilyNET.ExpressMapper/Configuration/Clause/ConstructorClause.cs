using System.Reflection;
using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Configuration.Config.Clause;

/// <summary>
/// Implementation of the constructor clause.
/// 构造函数子句的实现。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public class ConstructorClause<TSource, TDest> : IConstructClause<TSource, TDest>
{
    private readonly ICollection<MappingMember> _ctorParams = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstructorClause{TSource, TDest}" /> class.
    /// 初始化 <see cref="ConstructorClause{TSource, TDest}" /> 类的新实例。
    /// </summary>
    /// <param name="info">The constructor information. 构造函数信息。</param>
    public ConstructorClause(ConstructorInfo? info)
    {
        ConstructorInfo = info;
        IsValidClause = DefineIfValidClause(ConstructorInfo);
        if (IsValidClause)
        {
            InitConstructorParams();
        }
    }

    /// <summary>
    /// Gets the constructor information.
    /// 获取构造函数信息。
    /// </summary>
    public ConstructorInfo? ConstructorInfo { get; }

    /// <summary>
    /// Gets the constructor parameters.
    /// 获取构造函数参数。
    /// </summary>
    public IEnumerable<MappingMember> ConstructorParams => _ctorParams;

    /// <summary>
    /// Gets a value indicating whether this clause is valid.
    /// 获取一个值，该值指示此子句是否有效。
    /// </summary>
    public bool IsValidClause { get; }

    /// <summary>
    /// Defines if the clause is valid based on the constructor information.
    /// 根据构造函数信息定义子句是否有效。
    /// </summary>
    /// <param name="constructorInfo">The constructor information. 构造函数信息。</param>
    /// <returns>True if the clause is valid; otherwise, false. 如果子句有效，则为 true；否则为 false。</returns>
    private static bool DefineIfValidClause(ConstructorInfo? constructorInfo) => constructorInfo?.IsPublic ?? false;

    /// <summary>
    /// Initializes the constructor parameters.
    /// 初始化构造函数参数。
    /// </summary>
    private void InitConstructorParams()
    {
        var parameters = ConstructorInfo!.GetParameters();
        foreach (var parameter in parameters)
        {
            _ctorParams.Add(new()
            {
                Info = parameter.Member,
                Name = parameter.Name!,
                Type = parameter.ParameterType
            });
        }
    }
}