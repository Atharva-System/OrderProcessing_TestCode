namespace OrderProcessing.Domain.ValueObjects;

public record OrderItem
{
    public string ProductId { get; }
    public string ProductName { get; }
    public int ProductAmount { get; }
    public decimal ProductPrice { get; }
    public decimal TotalPrice => ProductAmount * ProductPrice;

    private OrderItem(string productId, string productName, int productAmount, decimal productPrice)
    {
        ProductId = productId;
        ProductName = productName;
        ProductAmount = productAmount;
        ProductPrice = productPrice;
    }

    public static OrderItem Create(string productId, string productName, int productAmount, decimal productPrice)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));
        
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be null or empty", nameof(productName));

        if (productAmount <= 0)
            throw new ArgumentException("Product amount must be greater than zero", nameof(productAmount));

        if (productPrice <= 0)
            throw new ArgumentException("Product price must be greater than zero", nameof(productPrice));

        return new OrderItem(productId.Trim(), productName.Trim(), productAmount, productPrice);
    }
}