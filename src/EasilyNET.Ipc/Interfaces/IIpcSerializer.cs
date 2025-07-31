using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// Defines methods for serializing and deserializing <see cref="IpcCommand" /> and <see cref="IpcCommandResponse" />
/// objects to and from byte arrays.
/// </summary>
/// <remarks>
/// This interface is designed to facilitate the conversion of inter-process communication (IPC) commands
/// and responses into a format suitable for transmission or storage, and vice versa. Implementations of this interface
/// must ensure that the serialization and deserialization processes are consistent and handle invalid or malformed data
/// gracefully.
/// </remarks>
public interface IIpcSerializer
{
    /// <summary>
    /// Serializes the specified <see cref="IpcCommand" /> into a byte array.
    /// </summary>
    /// <param name="command">The <see cref="IpcCommand" /> to serialize. Cannot be <see langword="null" />.</param>
    /// <returns>A byte array representing the serialized form of the <paramref name="command" />.</returns>
    byte[] SerializeCommand(IpcCommand command);

    /// <summary>
    /// Deserializes the provided byte array into an <see cref="IpcCommand" /> object.
    /// </summary>
    /// <param name="data">The byte array containing the serialized command data. Must not be null or empty.</param>
    /// <returns>An <see cref="IpcCommand" /> object if deserialization is successful; otherwise, <see langword="null" />.</returns>
    IpcCommand? DeserializeCommand(byte[] data);

    /// <summary>
    /// Serializes the specified <see cref="IpcCommandResponse" /> into a byte array.
    /// </summary>
    /// <remarks>
    /// Use this method to convert an <see cref="IpcCommandResponse" /> object into a format suitable
    /// for transmission or storage. The caller is responsible for ensuring the input is valid and non-null.
    /// </remarks>
    /// <param name="response">The <see cref="IpcCommandResponse" /> to serialize. Cannot be <see langword="null" />.</param>
    /// <returns>A byte array representing the serialized form of the <paramref name="response" />.</returns>
    byte[] SerializeResponse(IpcCommandResponse response);

    /// <summary>
    /// Deserializes the provided byte array into an <see cref="IpcCommandResponse" /> object.
    /// </summary>
    /// <param name="data">The byte array containing the serialized response data. Must not be null or empty.</param>
    /// <returns>
    /// An <see cref="IpcCommandResponse" /> object representing the deserialized response,  or <see langword="null" /> if
    /// the deserialization fails or the data is invalid.
    /// </returns>
    IpcCommandResponse? DeserializeResponse(byte[] data);
}