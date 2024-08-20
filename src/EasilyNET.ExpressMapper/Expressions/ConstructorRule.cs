using System.Reflection;
using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 构造函数规则类，用于定义构造函数参数和信息的映射规则。
/// Class for constructor rules, used to define mapping rules for constructor parameters and information.
/// </summary>
public class ConstructorRule : IMappingRule
{
    /// <summary>
    /// 构造函数参数的类型和映射成员的元组数组。
    /// An array of tuples containing the type of the constructor parameters and the mapping members.
    /// </summary>
    public required Tuple<Type, MappingMember?>[] ConstructorParams { get; init; }

    /// <summary>
    /// 构造函数信息。
    /// The constructor information.
    /// </summary>
    public required ConstructorInfo Info { get; init; }
}