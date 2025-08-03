using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using MessagePack;

namespace EasilyNET.Ipc.Serializers;

/// <summary>
/// Provides methods for serializing and deserializing inter-process communication (IPC) commands and responses using
/// MessagePack format for high performance binary serialization.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IIpcSerializer" /> interface and uses MessagePack serialization to
/// convert IPC commands and responses to and from byte arrays. MessagePack provides faster serialization/deserialization
/// and smaller binary size compared to JSON, making it ideal for high-performance IPC scenarios.
/// </remarks>
public sealed class MessagePackIpcSerializer : IIpcSerializer
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
                                                                                               .WithCompression(MessagePackCompression.Lz4BlockArray);

    /// <summary>
    /// Serializes the specified <see cref="IpcCommand" /> into a MessagePack byte array.
    /// </summary>
    /// <param name="command">The <see cref="IpcCommand" /> instance to serialize. Cannot be <see langword="null" />.</param>
    /// <returns>A byte array containing the MessagePack representation of the <paramref name="command" />.</returns>
    public byte[] SerializeCommand(IpcCommand command) => MessagePackSerializer.Serialize(command, Options);

    /// <summary>
    /// Deserializes a byte array into an <see cref="IpcCommand" /> object.
    /// </summary>
    /// <param name="data">The byte array containing the serialized MessagePack representation of an <see cref="IpcCommand" />.</param>
    /// <returns>An <see cref="IpcCommand" /> object if deserialization is successful; otherwise, <see langword="null" />.</returns>
    public IpcCommand? DeserializeCommand(byte[] data)
    {
        try
        {
            return MessagePackSerializer.Deserialize<IpcCommand>(data, Options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes the specified <see cref="IpcCommandResponse" /> object into a MessagePack byte array.
    /// </summary>
    /// <param name="response">The <see cref="IpcCommandResponse" /> object to serialize. Cannot be <see langword="null" />.</param>
    /// <returns>A byte array containing the MessagePack representation of the <paramref name="response" />.</returns>
    public byte[] SerializeResponse(IpcCommandResponse response) => MessagePackSerializer.Serialize(response, Options);

    /// <summary>
    /// Deserializes a byte array into an <see cref="IpcCommandResponse" /> object.
    /// </summary>
    /// <param name="data">The byte array containing the serialized MessagePack representation of an <see cref="IpcCommandResponse" />.</param>
    /// <returns>An <see cref="IpcCommandResponse" /> object if deserialization is successful; otherwise, <see langword="null" />.</returns>
    public IpcCommandResponse? DeserializeResponse(byte[] data)
    {
        try
        {
            return MessagePackSerializer.Deserialize<IpcCommandResponse>(data, Options);
        }
        catch
        {
            return null;
        }
    }
}