namespace EasilyNET.Core.Entities;

/// <summary>
/// 领域事件
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }

    DateTime DateTime { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime DateTime { get; } = DateTime.Now;
}