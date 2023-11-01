namespace EasilyNET.Core.Domains.Commands;

/// <summary>
/// 查询处理器
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }