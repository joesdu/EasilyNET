// ReSharper disable UnusedType.Global

namespace EasilyNET.ExpressMapper;

/// <summary>
/// Represents a key for mapping between source and destination types.
/// 表示源类型和目标类型之间映射的键。
/// </summary>
public readonly record struct MapKey(Type SourceType, Type DestType)
{
    /// <summary>
    /// Creates a new MapKey instance for the specified source and destination types.
    /// 为指定的源类型和目标类型创建一个新的 MapKey 实例。
    /// </summary>
    public static MapKey Form<TSource, TDest>() => new(typeof(TSource), typeof(TDest));
}

/// <summary>
/// Interface for keeping a mapping key for specific source and destination types.
/// 用于保存特定源类型和目标类型的映射键的接口。
/// </summary>
public interface IKeyKeeper<TSource, TDest> : IKeyKeeper;

/// <summary>
/// Interface for keeping a mapping key.
/// 用于保存映射键的接口。
/// </summary>
public interface IKeyKeeper
{
    /// <summary>
    /// Gets the mapping key.
    /// 获取映射键。
    /// </summary>
    public MapKey Key { get; }
}