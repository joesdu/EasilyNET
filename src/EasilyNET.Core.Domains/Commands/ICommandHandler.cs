namespace EasilyNET.Core.Domains.Commands;

/// <summary>
/// 命令处理器
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }