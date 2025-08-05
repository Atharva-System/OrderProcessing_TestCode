namespace OrderProcessing.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
    Guid EventId { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}