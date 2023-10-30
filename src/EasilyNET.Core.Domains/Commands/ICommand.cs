namespace EasilyNET.Core.Domains.Commands;


/// <summary>
/// 命令
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}