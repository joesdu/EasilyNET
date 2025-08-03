using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// 命令类型注册表，基于类型哈希而非字符串
/// </summary>
public sealed class CommandTypeRegistry : ICommandTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _typeHashToType = new();
    private readonly ConcurrentDictionary<Type, string> _typeToHash = new();

    /// <summary>
    /// 注册命令类型
    /// </summary>
    public void Register<TCommand>() where TCommand : ITypedCommand
    {
        var commandType = typeof(TCommand);
        var typeHash = GenerateTypeHash(commandType);
        _typeHashToType[typeHash] = commandType;
        _typeToHash[commandType] = typeHash;
    }

    /// <summary>
    /// 根据类型哈希获取命令类型
    /// </summary>
    public Type? GetCommandType(string typeHash) => _typeHashToType.GetValueOrDefault(typeHash);

    /// <summary>
    /// 获取命令类型哈希
    /// </summary>
    public string GetTypeHash(Type commandType)
    {
        if (_typeToHash.TryGetValue(commandType, out var hash))
        {
            return hash;
        }

        // 如果未注册，动态生成哈希
        hash = GenerateTypeHash(commandType);
        _typeHashToType[hash] = commandType;
        _typeToHash[commandType] = hash;
        return hash;
    }

    /// <summary>
    /// 批量注册程序集中的所有命令类型
    /// </summary>
    public void RegisterFromAssembly(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
                                   .Where(t => t.IsClass && !t.IsAbstract && typeof(ITypedCommand).IsAssignableFrom(t))
                                   .ToList();
        foreach (var commandType in commandTypes)
        {
            var typeHash = GenerateTypeHash(commandType);
            _typeHashToType[typeHash] = commandType;
            _typeToHash[commandType] = typeHash;
        }
    }

    /// <summary>
    /// 生成类型哈希，基于完整类型名和程序集版本
    /// </summary>
    private static string GenerateTypeHash(Type type)
    {
        // 使用类型全名 + 程序集版本生成稳定的哈希
        var typeInfo = $"{type.FullName}|{type.Assembly.GetName().Version}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(typeInfo));

        // 取前8字节转换为Base64，保证足够短且唯一
        return Convert.ToBase64String(hashBytes[..8]);
    }

    /// <summary>
    /// 获取所有已注册的命令类型
    /// </summary>
    public IReadOnlyDictionary<string, Type> GetAllRegisteredTypes() => _typeHashToType;
}

/// <summary>
/// 命令类型注册表接口
/// </summary>
public interface ICommandTypeRegistry
{
    /// <summary>
    /// 注册一个类型化命令类型到注册表中
    /// </summary>
    /// <typeparam name="TCommand">要注册的命令类型，必须实现ITypedCommand接口</typeparam>
    void Register<TCommand>() where TCommand : ITypedCommand;

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
}