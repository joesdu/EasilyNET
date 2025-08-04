using System.Reflection;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// 命令类型注册表接口
/// </summary>
public interface IIpcCommandRegistry
{
    /// <summary>
    /// 注册一个类型化命令类型到注册表中
    /// </summary>
    /// <typeparam name="TCommand">要注册的命令类型，必须实现ITypedCommand接口</typeparam>
    void Register<TCommand>() where TCommand : IIpcCommandBase;

    /// <summary>
    /// 根据类型哈希值获取对应的命令类型
    /// </summary>
    /// <param name="typeHash">类型的哈希值字符串</param>
    /// <returns>对应的命令类型，如果未找到则返回null</returns>
    Type? GetCommandType(string typeHash);

    /// <summary>
    /// 获取指定命令类型的哈希值
    /// </summary>
    /// <param name="commandType">命令类型</param>
    /// <returns>类型对应的哈希值字符串</returns>
    string GetTypeHash(Type commandType);

    /// <summary>
    /// 批量注册程序集中的所有命令类型
    /// </summary>
    /// <param name="assembly">要扫描的程序集</param>
    void RegisterFromAssembly(Assembly assembly);
}