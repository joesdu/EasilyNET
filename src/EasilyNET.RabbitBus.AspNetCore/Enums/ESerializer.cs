namespace EasilyNET.RabbitBus.AspNetCore.Enums;

/// <summary>
/// Serialization methods.
/// </summary>
public enum ESerializer
{
    /// <summary>
    /// Serialize using System.Text.Json.
    /// </summary>
    TextJson,

    /// <summary>
    /// Serialize using MessagePack.
    /// </summary>
    MessagePack
}