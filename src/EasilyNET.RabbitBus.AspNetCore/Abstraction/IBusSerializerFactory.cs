namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// Factory interface for creating bus serializers.
/// </summary>
internal interface IBusSerializerFactory
{
    /// <summary>
    /// Creates a new instance of a bus serializer.
    /// </summary>
    /// <returns>An instance of <see cref="IBusSerializer"/>.</returns>
    IBusSerializer CreateSerializer();
}