using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Exceptions;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 表达式构建器接口。
/// Interface for expression builder.
/// </summary>
public interface IExpressionBuilder
{
    /// <summary>
    /// 构建映射表达式。
    /// Builds a mapping expression.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="rules">映射规则集合。Collection of mapping rules.</param>
    /// <returns>映射表达式。Mapping expression.</returns>
    public Expression<Func<TSource, TDest>> BuildExpression<TSource, TDest>(IEnumerable<IMappingRule> rules);
}

/// <summary>
/// 表达式构建器类，用于构建源类型和目标类型之间的映射表达式。
/// Class for expression builder, used to build mapping expressions between source and destination types.
/// </summary>
public class ExpressionBuilder : IExpressionBuilder
{
    /// <summary>
    /// 构建映射表达式。
    /// Builds a mapping expression.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="rules">映射规则集合。Collection of mapping rules.</param>
    /// <returns>映射表达式。Mapping expression.</returns>
    public Expression<Func<TSource, TDest>> BuildExpression<TSource, TDest>(IEnumerable<IMappingRule> rules)
    {
        var rulesArray = rules.ToArray();
        var source = FormParameter<TSource>();
        var destination = FormVariable<TDest>();
        var assignProperties = AssignProperties(destination, source, rulesArray);
        var body = Expression.Block(new[] { destination }, assignProperties);
        return Expression.Lambda<Func<TSource, TDest>>(body, source);
    }

    /// <summary>
    /// 创建目标类型的变量表达式。
    /// Forms a variable expression for the destination type.
    /// </summary>
    private static ParameterExpression FormVariable<TDest>() => Expression.Variable(typeof(TDest), nameof(TDest).ToLower());

    /// <summary>
    /// 创建源类型的参数表达式。
    /// Forms a parameter expression for the source type.
    /// </summary>
    private static ParameterExpression FormParameter<TSource>() => Expression.Parameter(typeof(TSource), nameof(TSource).ToLower());

    /// <summary>
    /// 创建目标类型的新表达式。
    /// Forms a new expression for the destination type.
    /// </summary>
    private static NewExpression NewDestination(Type destinationType, ParameterExpression source, IEnumerable<IMappingRule> rules)
    {
        var ctorRule = rules.OfType<ConstructorRule>().FirstOrDefault();
        if (ctorRule is not null)
        {
            var paramsExpressions = new Expression[ctorRule.ConstructorParams.Length];
            var count = 0;
            foreach (var param in ctorRule.ConstructorParams)
            {
                if (param.Item2 is not null)
                {
                    paramsExpressions[count] = Expression.PropertyOrField(source, param.Item2.Name);
                }
                else
                {
                    paramsExpressions[count] = ExpressionHelper.DefaultValue(param.Item1);
                }
                count++;
            }
            return Expression.New(ctorRule.Info, paramsExpressions);
        }
        var ctor = destinationType.GetConstructor(Array.Empty<Type>());
        if (ctor is null)
            throw new CannotFindConstructorWithoutParamsException(destinationType);
        return Expression.New(ctor, Array.Empty<Expression>());
    }

    /// <summary>
    /// 分配属性的表达式。
    /// Assigns properties expressions.
    /// </summary>
    private static IEnumerable<Expression> AssignProperties(Expression destination, ParameterExpression source, IEnumerable<IMappingRule> rules)
    {
        var mappedRules = rules.ToArray();
        yield return Expression.Assign(destination, NewDestination(destination.Type, source, mappedRules));
        foreach (var autoAssignExpression in AssignAutoProperties(destination, source, mappedRules.OfType<AutoMappingRule>()))
            yield return autoAssignExpression;
        foreach (var expression in AssignMappedByClauseProperties(destination, source, mappedRules.OfType<MapClauseRule>()))
        {
            yield return expression;
        }
        yield return destination;
    }

    /// <summary>
    /// 分配自动映射属性的表达式。
    /// Assigns auto-mapping properties expressions.
    /// </summary>
    private static IEnumerable<Expression> AssignAutoProperties(Expression destination, ParameterExpression source, IEnumerable<IMappingRule> rules)
    {
        var autoMappingRules = rules.OfType<AutoMappingRule>();
        foreach (var autoMappingRule in autoMappingRules)
        {
            var destMember = Expression.PropertyOrField(destination, autoMappingRule.DestinationMember.Name);
            var sourceMember = Expression.PropertyOrField(source, autoMappingRule.SourceMember.Name);
            yield return Expression.Assign(destMember, sourceMember);
        }
    }

    /// <summary>
    /// 分配按子句映射的属性表达式。
    /// Assigns properties expressions mapped by clause.
    /// </summary>
    private static IEnumerable<Expression> AssignMappedByClauseProperties(Expression destination, ParameterExpression source, IEnumerable<IMappingRule> rules)
    {
        var mappedByClause = rules.OfType<MapClauseRule>();
        foreach (var mapRule in mappedByClause)
        {
            var destMember = Expression.PropertyOrField(destination, mapRule.DestinationMember.Name);
            var lambda = mapRule.SourceLambda;
            var lambdaCall = Expression.Invoke(lambda, source);
            yield return Expression.Assign(destMember, lambdaCall);
        }
    }
}