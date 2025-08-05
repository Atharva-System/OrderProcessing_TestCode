namespace OrderProcessing.Domain.ValueObjects;

public record OrderNumber
{
    public string Value { get; }

    private OrderNumber(string value)
    {
        Value = value;
    }

    public static OrderNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Order number cannot be null or empty", nameof(value));

        if (value.Length < 3 || value.Length > 30)
            throw new ArgumentException("Order number must be between 3 and 30 characters", nameof(value));

        return new OrderNumber(value);
    }

    public static OrderNumber Generate()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Random.Shared.Next(1000, 9999);
        return new OrderNumber($"ORD-{timestamp}-{random}");
    }

    public static implicit operator string(OrderNumber orderNumber) => orderNumber.Value;
    public override string ToString() => Value;
}
