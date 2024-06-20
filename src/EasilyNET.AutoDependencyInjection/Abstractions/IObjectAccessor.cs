namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 对象存取器
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IObjectAccessor<T>
{
    /// <summary>
    /// 值
    /// </summary>
    internal T? Value { get; set; }
}