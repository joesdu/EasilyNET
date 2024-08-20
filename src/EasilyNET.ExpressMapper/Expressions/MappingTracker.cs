using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Configuration.Config.Clause;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 映射跟踪器接口，用于跟踪源类型和目标类型之间的映射规则。
/// Interface for mapping tracker, used to track mapping rules between source and destination types.
/// </summary>
public interface IMappingTracker<TSource, TDest>
{
    /// <summary>
    /// 获取映射规则集合。
    /// Gets the collection of mapping rules.
    /// </summary>
    IEnumerable<IMappingRule> GetMappingRules();

    /// <summary>
    /// 移除被忽略的成员。
    /// Removes ignored members.
    /// </summary>
    IMappingTracker<TSource, TDest> RemoveIgnored();

    /// <summary>
    /// 映射构造函数参数。
    /// Maps constructor parameters.
    /// </summary>
    IMappingTracker<TSource, TDest> MapConstructorParams();

    /// <summary>
    /// 按名称自动映射。
    /// Auto maps by name.
    /// </summary>
    IMappingTracker<TSource, TDest> AutoMapByName();

    /// <summary>
    /// 按子句映射。
    /// Maps by clauses.
    /// </summary>
    IMappingTracker<TSource, TDest> MapByClauses();
}

/// <summary>
/// 映射跟踪器类，用于跟踪源类型和目标类型之间的映射规则。
/// Class for mapping tracker, used to track mapping rules between source and destination types.
/// </summary>
public class MappingTracker<TSource, TDest>(IConfig<TSource, TDest>? config) : IMappingTracker<TSource, TDest>
{
    private readonly ICollection<IMappingRule> _mappingRules = [];
    private List<MappingMember> _destinationMembers = MemberSearchHelper.FindAllMembers<TDest>(true).ToList();
    private List<MappingMember> _sourceMembers = MemberSearchHelper.FindAllMembers<TSource>().ToList();

    /// <summary>
    /// 获取映射规则集合。
    /// Gets the collection of mapping rules.
    /// </summary>
    public IEnumerable<IMappingRule> GetMappingRules() => _mappingRules;

    /// <summary>
    /// 移除被忽略的成员。
    /// Removes ignored members.
    /// </summary>
    public IMappingTracker<TSource, TDest> RemoveIgnored()
    {
        var sourceIgnoreMembers = ClauseHelper.GetSourceIgnoreMembers(config);
        var destIgnoreMembers = ClauseHelper.GetDestIgnoreMembers(config);
        _sourceMembers = _sourceMembers.Where(mm => !sourceIgnoreMembers.Contains(mm)).ToList();
        _destinationMembers = _destinationMembers.Where(mm => !destIgnoreMembers.Contains(mm)).ToList();
        return this;
    }

    /// <summary>
    /// 映射构造函数参数。
    /// Maps constructor parameters.
    /// </summary>
    public IMappingTracker<TSource, TDest> MapConstructorParams()
    {
        var constructorClause = ClauseHelper.GetConstructorClause(config);
        // ReSharper disable once InvertIf
        if (constructorClause is not null)
        {
            var ctorRule = new ConstructorRule
            {
                ConstructorParams = constructorClause.ConstructorParams
                                                     .Select(p => new Tuple<Type, MappingMember?>(p.Type,
                                                         _sourceMembers.SingleOrDefault(sm => sm.Name == NameHelper.ToUpperFirstLatter(p.Name) && sm.Type == p.Type))).ToArray(),
                Info = constructorClause.ConstructorInfo!
            };
            _mappingRules.Add(ctorRule);
        }
        return this;
    }

    /// <summary>
    /// 按名称自动映射。
    /// Auto maps by name.
    /// </summary>
    public IMappingTracker<TSource, TDest> AutoMapByName()
    {
        var enumerable = _destinationMembers
                         .Where(dm => _sourceMembers.Any(sm => sm.Name == dm.Name && sm.Type == dm.Type))
                         .Select(dm => new
                         {
                             SourceMember = _sourceMembers.Single(sm => sm.Name == dm.Name && dm.Type == sm.Type),
                             DestinationMember = dm
                         }).ToList();
        foreach (var container in enumerable)
        {
            _sourceMembers.Remove(container.SourceMember);
            _destinationMembers.Remove(container.DestinationMember);
            _mappingRules.Add(new AutoMappingRule
            {
                SourceMember = container.SourceMember,
                DestinationMember = container.DestinationMember
            });
        }
        return this;
    }

    /// <summary>
    /// 按子句映射。
    /// Maps by clauses.
    /// </summary>
    public IMappingTracker<TSource, TDest> MapByClauses()
    {
        var mapClauses = ClauseHelper.GetMapClauses(config);
        foreach (var mapClause in mapClauses)
        {
            // ! - GetMapClauses() guarantees that clauses are valid
            _destinationMembers.Remove(mapClause.DestinationMember!);
            _mappingRules.Add(new MapClauseRule
            {
                DestinationMember = mapClause.DestinationMember!,
                SourceLambda = mapClause.Expression!
            });
        }
        return this;
    }
}