namespace EasilyNET.Core.Entities;

/// <summary>
/// 领域事件
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// ID
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Time
    /// </summary>
    DateTime DateTime { get; }
}

/// <inheritdoc />
public abstract class DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime DateTime { get; } = DateTime.Now;
}