using OrderProcessing.Domain.Common;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Domain.Entities;

public class Order : BaseEntity
{
    public OrderNumber OrderNumber { get; private set; }
    public string InvoiceEmailAddress { get; private set; }
    public InvoiceAddress InvoiceAddress { get; private set; }
    public CreditCardNumber InvoiceCreditCardNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount => _items.Sum(item => item.TotalPrice);
    public string? Notes { get; private set; }
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // For EF Core

    private Order(OrderNumber orderNumber, string invoiceEmailAddress, InvoiceAddress invoiceAddress, 
                  CreditCardNumber invoiceCreditCardNumber, List<OrderItem> items, string? notes = null)
    {
        OrderNumber = orderNumber;
        InvoiceEmailAddress = invoiceEmailAddress;
        InvoiceAddress = invoiceAddress;
        InvoiceCreditCardNumber = invoiceCreditCardNumber;
        Status = OrderStatus.Pending;
        Notes = notes;
        
        foreach (var item in items)
        {
            _items.Add(item);
        }
    }

    public static Order Create(string invoiceEmailAddress, InvoiceAddress invoiceAddress, 
                              CreditCardNumber invoiceCreditCardNumber, List<OrderItem> items, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceEmailAddress))
            throw new ArgumentException("Invoice email address cannot be null or empty", nameof(invoiceEmailAddress));

        if (!IsValidEmail(invoiceEmailAddress))
            throw new ArgumentException("Invalid email format", nameof(invoiceEmailAddress));

        if (invoiceAddress == null)
            throw new ArgumentNullException(nameof(invoiceAddress));

        if (invoiceCreditCardNumber == null)
            throw new ArgumentNullException(nameof(invoiceCreditCardNumber));

        if (items == null || !items.Any())
            throw new ArgumentException("Order must have at least one item", nameof(items));

        // Business rule: Maximum order value
        var totalValue = items.Sum(i => i.TotalPrice);
        if (totalValue > 100000m)
            throw new InvalidOperationException("Order total cannot exceed $100,000");

        // Business rule: Maximum items per order
        if (items.Count > 50)
            throw new InvalidOperationException("Order cannot contain more than 50 different products");

        var orderNumber = OrderNumber.Generate();
        return new Order(orderNumber, invoiceEmailAddress.Trim().ToLowerInvariant(), invoiceAddress, 
                        invoiceCreditCardNumber, items, notes);
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");

        Status = newStatus;
        SetUpdatedAt();
    }

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to non-pending orders");

        if (_items.Count >= 50)
            throw new InvalidOperationException("Cannot add more than 50 items to an order");

        _items.Add(item);
        SetUpdatedAt();
    }

    public void RemoveItem(string productId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot remove items from non-pending orders");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
            SetUpdatedAt();
        }
    }

    public void UpdateItemQuantity(string productId, int newQuantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify items in non-pending orders");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new ArgumentException($"Product {productId} not found in order");

        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero");

        // Remove and re-add with new quantity (since OrderItem is immutable)
        _items.Remove(item);
        _items.Add(OrderItem.Create(item.ProductId, item.ProductName, newQuantity, item.ProductPrice));
        SetUpdatedAt();
    }

    public void UpdateNotes(string? notes)
    {
        if (notes?.Length > 1000)
            throw new ArgumentException("Notes cannot exceed 1000 characters");

        Notes = notes;
        SetUpdatedAt();
    }

    public bool ContainsProduct(string productId)
    {
        return _items.Any(i => i.ProductId == productId);
    }

    public int GetProductQuantity(string productId)
    {
        return _items.FirstOrDefault(i => i.ProductId == productId)?.ProductAmount ?? 0;
    }

    private bool CanTransitionTo(OrderStatus newStatus)
    {
        return newStatus switch
        {
            OrderStatus.Pending => false, // Cannot go back to pending
            OrderStatus.Confirmed => Status == OrderStatus.Pending,
            OrderStatus.Processing => Status is OrderStatus.Confirmed or OrderStatus.Processing,
            OrderStatus.Shipped => Status is OrderStatus.Processing or OrderStatus.Shipped,
            OrderStatus.Delivered => Status == OrderStatus.Shipped,
            OrderStatus.Cancelled => Status is OrderStatus.Pending or OrderStatus.Confirmed,
            _ => false
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
