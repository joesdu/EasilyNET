namespace EasilyNET.RabbitBus.AspNetCore.Enums;

/// <summary>
/// Types of message handlers.
/// </summary>
internal enum EKindOfHandler
{
    /// <summary>
    /// Normal message handler.
    /// </summary>
    Normal,

    /// <summary>
    /// Delayed message handler.
    /// </summary>
    Delayed
}