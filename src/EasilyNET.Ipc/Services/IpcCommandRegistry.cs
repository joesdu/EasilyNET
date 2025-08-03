using System.Collections.Concurrent;
using EasilyNET.Ipc.Abstractions;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// IPC 命令类型注册表
/// </summary>
public class IpcCommandRegistry
{
    private readonly ConcurrentDictionary<string, IIpcCommandMetadata> _commandTypes = new();
    private readonly ConcurrentDictionary<Type, string> _typeToName = new();

    /// <summary>
    /// 注册命令类型
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <typeparam name="TPayload">负载数据类型</typeparam>
    /// <param name="commandTypeName">命令类型名称</param>
    /// <param name="version">版本号</param>
    public void RegisterCommand<TCommand, TPayload>(string? commandTypeName = null, int version = 1)
        where TCommand : class, IIpcCommand<TPayload>
    {
        var typeName = commandTypeName ?? typeof(TCommand).Name;
        var metadata = new CommandMetadata(typeName, version, typeof(TPayload));
        _commandTypes.AddOrUpdate(typeName, metadata, (_, _) => metadata);
        _typeToName.AddOrUpdate(typeof(TCommand), typeName, (_, _) => typeName);
    }

    /// <summary>
    /// 根据命令类型获取元数据
    /// </summary>
    /// <param name="commandTypeName">命令类型名称</param>
    /// <returns>命令元数据</returns>
    public IIpcCommandMetadata? GetMetadata(string commandTypeName) => _commandTypes.TryGetValue(commandTypeName, out var metadata) ? metadata : null;

    /// <summary>
    /// 根据命令实例类型获取类型名称
    /// </summary>
    /// <param name="commandType">命令类型</param>
    /// <returns>命令类型名称</returns>
    public string? GetCommandTypeName(Type commandType) => _typeToName.TryGetValue(commandType, out var typeName) ? typeName : null;

    /// <summary>
    /// 获取所有已注册的命令类型
    /// </summary>
    /// <returns>命令类型名称集合</returns>
    public IEnumerable<string> GetRegisteredCommandTypes() => _commandTypes.Keys;

    /// <summary>
    /// 检查命令类型是否已注册
    /// </summary>
    /// <param name="commandTypeName">命令类型名称</param>
    /// <returns>是否已注册</returns>
    public bool IsRegistered(string commandTypeName) => _commandTypes.ContainsKey(commandTypeName);

    private sealed class CommandMetadata : IIpcCommandMetadata
    {
        public CommandMetadata(string commandTypeName, int version, Type payloadType)
        {
            CommandTypeName = commandTypeName;
            Version = version;
            PayloadType = payloadType;
        }

        public string CommandTypeName { get; }

        public int Version { get; }

        public Type PayloadType { get; }
    }
}