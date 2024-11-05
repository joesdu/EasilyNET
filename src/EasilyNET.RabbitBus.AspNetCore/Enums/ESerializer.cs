namespace EasilyNET.RabbitBus.AspNetCore.Enums;

/// <summary>
/// 序列化方式
/// </summary>
public enum ESerializer
{
    /// <summary>
    /// 使用 System.Text.Json 进行序列化
    /// </summary>
    TextJson,

    /// <summary>
    /// 使用 MessagePack 进行序列化
    /// </summary>
    MessagePack
}