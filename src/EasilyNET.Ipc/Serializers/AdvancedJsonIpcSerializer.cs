using System.Text.Json;
using System.Text.Json.Serialization;
using EasilyNET.Ipc.Abstractions;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Services;

namespace EasilyNET.Ipc.Serializers;

/// <summary>
/// 高级 JSON IPC 序列化器
/// </summary>
public class AdvancedJsonIpcSerializer : IIpcGenericSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// 初始化新的序列化器实例
    /// </summary>
    /// <param name="options">JSON 序列化选项</param>
    public AdvancedJsonIpcSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ??
                   new JsonSerializerOptions
                   {
                       PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                       WriteIndented = false,
                       DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                   };
    }

    /// <inheritdoc />
    public byte[] SerializeCommand<TPayload>(IIpcCommand<TPayload> command, IpcCommandRegistry registry)
    {
        var commandTypeName = registry.GetCommandTypeName(command.GetType());
        if (commandTypeName == null)
        {
            throw new InvalidOperationException($"命令类型 {command.GetType().Name} 未在注册表中注册");
        }
        var envelope = new CommandEnvelope
        {
            CommandTypeName = commandTypeName,
            CommandId = command.CommandId,
            TargetId = command.TargetId,
            Timestamp = command.Timestamp,
            PayloadJson = JsonSerializer.Serialize(command.Payload, _options)
        };
        return JsonSerializer.SerializeToUtf8Bytes(envelope, _options);
    }

    /// <inheritdoc />
    public IIpcCommandBase? DeserializeCommand(byte[] data, IpcCommandRegistry registry)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<CommandEnvelope>(data, _options);
            if (envelope == null || string.IsNullOrEmpty(envelope.CommandTypeName))
            {
                return null;
            }
            var metadata = registry.GetMetadata(envelope.CommandTypeName);
            if (metadata == null)
            {
                throw new InvalidOperationException($"未知的命令类型: {envelope.CommandTypeName}");
            }

            // 反序列化负载数据
            var payload = JsonSerializer.Deserialize(envelope.PayloadJson, metadata.PayloadType, _options);

            // 创建泛型命令实例
            var commandType = typeof(DeserializedCommand<>).MakeGenericType(metadata.PayloadType);
            return (IIpcCommandBase?)Activator.CreateInstance(commandType,
                envelope.CommandId,
                payload,
                envelope.TargetId,
                envelope.Timestamp,
                envelope.CommandTypeName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public byte[] SerializeResponse<TData>(TData data) => JsonSerializer.SerializeToUtf8Bytes(data, _options);

    /// <inheritdoc />
    public TData? DeserializeResponse<TData>(byte[] data)
    {
        try
        {
            return JsonSerializer.Deserialize<TData>(data, _options);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// 命令传输包装器
    /// </summary>
    private class CommandEnvelope
    {
        public string CommandTypeName { get; set; } = string.Empty;

        public string CommandId { get; set; } = string.Empty;

        public string? TargetId { get; set; }

        public DateTime Timestamp { get; set; }

        public string PayloadJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// 反序列化后的命令实现
    /// </summary>
    private class DeserializedCommand<TPayload> : IIpcCommand<TPayload>
    {
        public DeserializedCommand(string commandId, TPayload payload, string? targetId, DateTime timestamp, string commandTypeName)
        {
            CommandId = commandId;
            Payload = payload;
            TargetId = targetId;
            Timestamp = timestamp;
            CommandTypeName = commandTypeName;
        }

        public string CommandTypeName { get; }

        public string CommandId { get; }

        public TPayload Payload { get; }

        public string? TargetId { get; }

        public DateTime Timestamp { get; }
    }
}