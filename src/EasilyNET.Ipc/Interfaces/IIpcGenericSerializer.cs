using EasilyNET.Ipc.Abstractions;
using EasilyNET.Ipc.Services;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// 泛型 IPC 序列化器接口
/// </summary>
public interface IIpcGenericSerializer
{
    /// <summary>
    /// 序列化命令到字节数组
    /// </summary>
    /// <typeparam name="TPayload">负载数据类型</typeparam>
    /// <param name="command">命令实例</param>
    /// <param name="registry">命令注册表</param>
    /// <returns>序列化后的字节数组</returns>
    byte[] SerializeCommand<TPayload>(IIpcCommand<TPayload> command, IpcCommandRegistry registry);

    /// <summary>
    /// 从字节数组反序列化命令
    /// </summary>
    /// <param name="data">序列化的数据</param>
    /// <param name="registry">命令注册表</param>
    /// <returns>反序列化后的命令</returns>
    IIpcCommandBase? DeserializeCommand(byte[] data, IpcCommandRegistry registry);

    /// <summary>
    /// 序列化响应数据
    /// </summary>
    /// <typeparam name="TData">响应数据类型</typeparam>
    /// <param name="data">响应数据</param>
    /// <returns>序列化后的字节数组</returns>
    byte[] SerializeResponse<TData>(TData data);

    /// <summary>
    /// 反序列化响应数据
    /// </summary>
    /// <typeparam name="TData">响应数据类型</typeparam>
    /// <param name="data">序列化的数据</param>
    /// <returns>反序列化后的响应数据</returns>
    TData? DeserializeResponse<TData>(byte[] data);
}