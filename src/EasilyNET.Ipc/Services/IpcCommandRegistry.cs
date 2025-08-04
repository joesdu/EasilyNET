using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EasilyNET.Ipc.Interfaces;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// IPC 命令类型注册表，同时实现简单和完整的命令注册功能
/// </summary>
public class IpcCommandRegistry : IIpcCommandRegistry
{
    private readonly ConcurrentDictionary<string, IIpcCommandMetadata> _commandTypes = new();
    private readonly ConcurrentDictionary<Type, string> _typeToName = new();

    /// <summary>
    /// 注册命令类型
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <typeparam name="TPayload">负载数据类型</typeparam>
    /// <param name="commandTypeName">命令类型名称</param>
    /// <param name="responseType">响应类型</param>
    public void RegisterCommand<TCommand, TPayload>(string? commandTypeName = null, Type? responseType = null)
        where TCommand : class, IIpcCommand<TPayload>
    {
        var typeName = commandTypeName ?? typeof(TCommand).Name;
        var typeHash = GenerateTypeHash(typeof(TCommand));
        var metadata = new CommandMetadata(typeof(TCommand), typeName, typeof(TPayload), responseType, typeHash);
        _commandTypes.AddOrUpdate(typeName, metadata, (_, _) => metadata);
        _typeToName.AddOrUpdate(typeof(TCommand), typeName, (_, _) => typeName);
    }

    /// <summary>
    /// 根据命令类型名称或类型哈希获取元数据
    /// </summary>
    /// <param name="commandTypeNameOrHash">命令类型名称或类型哈希</param>
    /// <returns>命令元数据</returns>
    public IIpcCommandMetadata? GetMetadata(string commandTypeNameOrHash)
    {
        // 首先尝试按命令类型名称查找
        if (_commandTypes.TryGetValue(commandTypeNameOrHash, out var metadata))
        {
            return metadata;
        }

        // 如果找不到，可能是类型哈希，尝试查找所有匹配的哈希
        var metadataByHash = _commandTypes.Values.FirstOrDefault(m => m.TypeHash == commandTypeNameOrHash);
        return metadataByHash;
    }

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

    /// <summary>
    /// 生成类型哈希值
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>哈希值</returns>
    private static string GenerateTypeHash(Type type)
    {
        var fullName = type.FullName ?? type.Name;
        var bytes = Encoding.UTF8.GetBytes(fullName);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // 取前16个字符
    }

    private sealed class CommandMetadata(Type commandType, string commandName, Type payloadType, Type? responseType, string typeHash) : IIpcCommandMetadata
    {
        public Type CommandType { get; } = commandType;

        public string CommandName { get; } = commandName;

        public Type PayloadType { get; } = payloadType;

        public Type? ResponseType { get; } = responseType;

        public string TypeHash { get; } = typeHash;
    }

    #region ICommandRegistry 接口实现

    /// <summary>
    /// 注册一个类型化命令类型到注册表中
    /// </summary>
    /// <typeparam name="TCommand">要注册的命令类型，必须实现IIpcCommandBase接口</typeparam>
    public void Register<TCommand>() where TCommand : IIpcCommandBase
    {
        var commandType = typeof(TCommand);
        var typeHash = GenerateTypeHash(commandType);
        var commandName = commandType.Name;

        // 如果是泛型命令，提取负载类型
        Type? payloadType = null;
        if (commandType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIpcCommand<>)) is { } genericInterface)
        {
            payloadType = genericInterface.GetGenericArguments()[0];
        }
        var metadata = new CommandMetadata(commandType, commandName, payloadType ?? typeof(object), null, typeHash);
        _commandTypes.AddOrUpdate(typeHash, metadata, (_, _) => metadata);
        _typeToName.AddOrUpdate(commandType, typeHash, (_, _) => typeHash);
    }

    /// <summary>
    /// 根据类型哈希值获取对应的命令类型
    /// </summary>
    /// <param name="typeHash">类型的哈希值字符串</param>
    /// <returns>对应的命令类型，如果未找到则返回null</returns>
    public Type? GetCommandType(string typeHash) => _commandTypes.TryGetValue(typeHash, out var metadata) ? metadata.CommandType : null;

    /// <summary>
    /// 获取指定命令类型的哈希值
    /// </summary>
    /// <param name="commandType">命令类型</param>
    /// <returns>类型对应的哈希值字符串</returns>
    public string GetTypeHash(Type commandType)
    {
        if (_typeToName.TryGetValue(commandType, out var hash))
        {
            return hash;
        }

        // 如果未注册，动态生成哈希并注册
        hash = GenerateTypeHash(commandType);
        Register(commandType, hash);
        return hash;
    }

    /// <summary>
    /// 批量注册程序集中的所有命令类型
    /// </summary>
    /// <param name="assembly">要扫描的程序集</param>
    public void RegisterFromAssembly(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
                                   .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IIpcCommandBase).IsAssignableFrom(t))
                                   .ToList();
        foreach (var commandType in commandTypes)
        {
            var typeHash = GenerateTypeHash(commandType);
            Register(commandType, typeHash);
        }
    }

    /// <summary>
    /// 动态注册命令类型
    /// </summary>
    private void Register(Type commandType, string typeHash)
    {
        var commandName = commandType.Name;

        // 如果是泛型命令，提取负载类型
        Type? payloadType = null;
        if (commandType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIpcCommand<>)) is { } genericInterface)
        {
            payloadType = genericInterface.GetGenericArguments()[0];
        }
        var metadata = new CommandMetadata(commandType, commandName, payloadType ?? typeof(object), null, typeHash);
        _commandTypes.AddOrUpdate(typeHash, metadata, (_, _) => metadata);
        _typeToName.AddOrUpdate(commandType, typeHash, (_, _) => typeHash);
    }

    #endregion
}