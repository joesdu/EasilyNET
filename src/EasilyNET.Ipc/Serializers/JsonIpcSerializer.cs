using System.Text;
using System.Text.Json;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Serializers;

/// <summary>
/// Provides methods for serializing and deserializing inter-process communication (IPC) commands and responses using
/// JSON format.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IIpcSerializer" /> interface and uses JSON serialization to
/// convert IPC commands and responses to and from byte arrays. It is designed to facilitate communication between
/// processes by encoding data in a format that is both human-readable and efficient for transmission.
/// </remarks>
public sealed class JsonIpcSerializer : IIpcSerializer
{
    /// <summary>
    /// Serializes the specified <see cref="IpcCommand" /> into a UTF-8 encoded JSON byte array.
    /// </summary>
    /// <param name="command">The <see cref="IpcCommand" /> instance to serialize. Cannot be <see langword="null" />.</param>
    /// <returns>A byte array containing the UTF-8 encoded JSON representation of the <paramref name="command" />.</returns>
    public byte[] SerializeCommand(IpcCommand command) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command));

    /// <summary>
    /// Deserializes a byte array into an <see cref="IpcCommand" /> object.
    /// </summary>
    /// <remarks>
    /// The input byte array is expected to contain a valid UTF-8 encoded JSON string representing an
    /// <see cref="IpcCommand" />.  If the data is invalid or cannot be deserialized, the method returns
    /// <see
    ///     langword="null" />
    /// instead of throwing an exception.
    /// </remarks>
    /// <param name="data">The byte array containing the serialized JSON representation of an <see cref="IpcCommand" />.</param>
    /// <returns>An <see cref="IpcCommand" /> object if deserialization is successful; otherwise, <see langword="null" />.</returns>
    public IpcCommand? DeserializeCommand(byte[] data) => JsonSerializer.Deserialize<IpcCommand>(Encoding.UTF8.GetString(data));

    /// <summary>
    /// Serializes the specified <see cref="IpcCommandResponse" /> object into a UTF-8 encoded JSON byte array.
    /// </summary>
    /// <param name="response">The <see cref="IpcCommandResponse" /> object to serialize. Cannot be <see langword="null" />.</param>
    /// <returns>A byte array containing the UTF-8 encoded JSON representation of the <paramref name="response" />.</returns>
    public byte[] SerializeResponse(IpcCommandResponse response) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

    /// <summary>
    /// Deserializes a byte array into an <see cref="IpcCommandResponse" /> object.
    /// </summary>
    /// <remarks>
    /// The method expects the input byte array to contain a valid UTF-8 encoded JSON string. If the
    /// input data is invalid or cannot be deserialized into an <see cref="IpcCommandResponse" />, the method returns
    /// <see langword="null" />.
    /// </remarks>
    /// <param name="data">The byte array containing the serialized JSON representation of an <see cref="IpcCommandResponse" />.</param>
    /// <returns>An <see cref="IpcCommandResponse" /> object if deserialization is successful; otherwise, <see langword="null" />.</returns>
    public IpcCommandResponse? DeserializeResponse(byte[] data) => JsonSerializer.Deserialize<IpcCommandResponse>(Encoding.UTF8.GetString(data));
}