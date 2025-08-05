namespace OrderProcessing.Domain.ValueObjects;

public record InvoiceAddress
{
    public string Value { get; }

    private InvoiceAddress(string value)
    {
        Value = value;
    }

    public static InvoiceAddress Create(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Invoice address cannot be null or empty", nameof(address));

        return new InvoiceAddress(address.Trim());
    }

    public override string ToString() => Value;
}