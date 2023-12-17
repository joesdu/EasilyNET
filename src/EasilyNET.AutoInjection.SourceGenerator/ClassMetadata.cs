using Microsoft.CodeAnalysis;

namespace EasilyNET.AutoInjection.SourceGenerator;

/// <summary>
/// 数元数据
/// </summary>
/// <param name="implementationType">实例类型</param>
/// <param name="lifetime">生命周期</param>
public sealed class ClassMetadata(ITypeSymbol implementationType, string lifetime)
{
    /// <summary>
    /// 实例类型
    /// </summary>
    public ITypeSymbol ImplementationType { get; } = implementationType;

    /// <summary>
    /// 生命周期
    /// </summary>
    public string Lifetime { get; } = lifetime;

    /// <summary>
    /// 服务类型集合
    /// </summary>
    public List<ITypeSymbol> ServiceTypes { get; } = [];

    /// <summary>
    /// 添加服务类型
    /// </summary>
    /// <param name="serviceTypes">服务类型集合</param>
    /// <returns></returns>
    public ClassMetadata AddServiceTypes(IEnumerable<ITypeSymbol> serviceTypes)
    {
        ServiceTypes.AddRange(serviceTypes);
        return this;
    }
}