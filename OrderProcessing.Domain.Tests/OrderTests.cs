using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.ValueObjects;
using Xunit;

namespace OrderProcessing.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Create_ValidOrder_ShouldCreateOrderWithPendingStatus()
    {
        // Arrange
        var invoiceEmailAddress = "karan@example.com";
        var invoiceAddress = InvoiceAddress.Create("123 Sample Street, 90402 Berlin");
        var creditCardNumber = CreditCardNumber.Create("1234-5678-9101-1121");
        var items = new List<OrderItem>
        {
            OrderItem.Create("12345", "Gaming Laptop", 2, 1499.99m)
        };

        // Act
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(invoiceEmailAddress, order.InvoiceEmailAddress);
        Assert.Equal(invoiceAddress.Value, order.InvoiceAddress.Value);
        Assert.Equal(creditCardNumber.Value, order.InvoiceCreditCardNumber.Value);
        Assert.Equal(2999.98m, order.TotalAmount);
        Assert.Equal(Domain.Enums.OrderStatus.Pending, order.Status);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.True(order.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_InvalidEmailAddress_ShouldThrowArgumentException()
    {
        // Arrange
        var invoiceAddress = InvoiceAddress.Create("123 Sample Street, 90402 Berlin");
        var creditCardNumber = CreditCardNumber.Create("1234-5678-9101-1121");
        var items = new List<OrderItem>
        {
            OrderItem.Create("12345", "Gaming Laptop", 2, 1499.99m)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Order.Create("", invoiceAddress, creditCardNumber, items));
        Assert.Throws<ArgumentException>(() => Order.Create("invalid-email", invoiceAddress, creditCardNumber, items));
    }

    [Fact]
    public void Create_EmptyItems_ShouldThrowArgumentException()
    {
        // Arrange
        var invoiceEmailAddress = "dinesh@example.com";
        var invoiceAddress = InvoiceAddress.Create("123 Sample Street, 90402 Berlin");
        var creditCardNumber = CreditCardNumber.Create("1234-5678-9101-1121");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, new List<OrderItem>()));
        Assert.Throws<ArgumentException>(() => Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, null!));
    }

    [Fact]
    public void UpdateStatus_ValidTransition_ShouldUpdateStatus()
    {
        // Arrange
        var invoiceEmailAddress = "manan@example.com";
        var invoiceAddress = InvoiceAddress.Create("123 Sample Street, 90402 Berlin");
        var creditCardNumber = CreditCardNumber.Create("1234-5678-9101-1121");
        var items = new List<OrderItem>
        {
            OrderItem.Create("12345", "Gaming Laptop", 1, 1499.99m)
        };
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items);

        // Act
        order.UpdateStatus(Domain.Enums.OrderStatus.Confirmed);

        // Assert
        Assert.Equal(Domain.Enums.OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    [Fact]
    public void UpdateStatus_InvalidTransition_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invoiceEmailAddress = "rutvik@example.com";
        var invoiceAddress = InvoiceAddress.Create("123 Sample Street, 90402 Berlin");
        var creditCardNumber = CreditCardNumber.Create("1234-5678-9101-1121");
        var items = new List<OrderItem>
        {
            OrderItem.Create("12345", "Gaming Laptop", 1, 1499.99m)
        };
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.UpdateStatus(Domain.Enums.OrderStatus.Delivered));
    }
}
