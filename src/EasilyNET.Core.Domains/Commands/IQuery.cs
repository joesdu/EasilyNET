namespace EasilyNET.Core.Domains.Commands;

/// <summary>
/// 查询器
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse> { }