namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// Interface for a serializer used in the bus.
/// </summary>
internal interface IBusSerializer
{
    /// <summary>
    /// Serializes an object to a byte array.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="type">The type of the object.</param>
    /// <returns>A byte array representing the serialized object.</returns>
    byte[] Serialize(object? obj, Type type);

    /// <summary>
    /// Deserializes a byte array to an object.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <param name="type">The target type of the deserialized object.</param>
    /// <returns>The deserialized object.</returns>
    object? Deserialize(byte[] data, Type type);
}