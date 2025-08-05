using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Domain.ValueObjects;
using OrderProcessing.Infrastructure.Data;
using OrderProcessing.Infrastructure.Repositories;

namespace OrderProcessing.Infrastructure.Tests.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly OrderProcessingDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrderProcessingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderProcessingDbContext(options);
        _repository = new OrderRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ValidOrder_ShouldSaveOrderToDatabase()
    {
        // Arrange
        var invoiceEmailAddress = "jatin@example.com";
        var invoiceAddress = InvoiceAddress.Create("123 Sample Street, 90402 Berlin");
        var creditCardNumber = CreditCardNumber.Create("1234-5678-9101-1121");
        var items = new List<OrderItem>
        {
            OrderItem.Create("12345", "Gaming Laptop", 2, 1499.99m),
            OrderItem.Create("67890", "Wireless Mouse", 1, 29.99m)
        };
        
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items, "Test order");

        // Act
        await _repository.AddAsync(order);

        // Assert
        var savedOrder = await _context.Orders.FirstOrDefaultAsync();
        savedOrder.Should().NotBeNull();
        savedOrder!.InvoiceEmailAddress.Should().Be("jatin@example.com");
        savedOrder.InvoiceAddress.Value.Should().Be("123 Sample Street, 90402 Berlin");
        savedOrder.InvoiceCreditCardNumber.Value.Should().Be("1234-5678-9101-1121");
        savedOrder.TotalAmount.Should().Be(3029.97m);
        savedOrder.Notes.Should().Be("Test order");
        savedOrder.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByOrderNumberAsync_ExistingOrder_ShouldReturnOrder()
    {
        // Arrange
        var invoiceEmailAddress = "jigar@example.com";
        var invoiceAddress = InvoiceAddress.Create("456 Main Street, 10001 New York");
        var creditCardNumber = CreditCardNumber.Create("9876-5432-1098-7654");
        var items = new List<OrderItem>
        {
            OrderItem.Create("11111", "Mechanical Keyboard", 1, 149.99m)
        };
        
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items);

        await _repository.AddAsync(order);
        var orderNumber = order.OrderNumber.Value;

        // Act
        var result = await _repository.GetByOrderNumberAsync(order.OrderNumber);

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Value.Should().Be(orderNumber);
        result.InvoiceEmailAddress.Should().Be("jigar@example.com");
        result.InvoiceAddress.Value.Should().Be("456 Main Street, 10001 New York");
        result.TotalAmount.Should().Be(149.99m);
        result.Items.Should().HaveCount(1);
        
        var item = result.Items.First();
        item.ProductName.Should().Be("Mechanical Keyboard");
        item.ProductAmount.Should().Be(1);
        item.ProductPrice.Should().Be(149.99m);
    }

    [Fact]
    public async Task GetByOrderNumberAsync_NonExistentOrder_ShouldReturnNull()
    {
        // Arrange
        var nonExistentOrderNumber = OrderNumber.Create("ORD-NONEXISTENT");

        // Act
        var result = await _repository.GetByOrderNumberAsync(nonExistentOrderNumber);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_OrderWithMinimalData_ShouldSaveSuccessfully()
    {
        // Arrange
        var invoiceEmailAddress = "shubham@example.com";
        var invoiceAddress = InvoiceAddress.Create("789 Oak Avenue, 60601 Chicago");
        var creditCardNumber = CreditCardNumber.Create("1111-2222-3333-4444");
        var items = new List<OrderItem>
        {
            OrderItem.Create("22222", "USB Cable", 3, 15.99m)
        };
        
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items, "Order with minimal data");

        // Act
        await _repository.AddAsync(order);

        // Assert
        var savedOrder = await _context.Orders.FirstOrDefaultAsync();
        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Should().HaveCount(1);
        savedOrder.TotalAmount.Should().Be(47.97m);
        savedOrder.Notes.Should().Be("Order with minimal data");
    }

    [Fact]
    public async Task AddAsync_OrderWithNullNotes_ShouldSaveSuccessfully()
    {
        // Arrange
        var invoiceEmailAddress = "parth@example.com";
        var invoiceAddress = InvoiceAddress.Create("321 Pine Street, 30301 Atlanta");
        var creditCardNumber = CreditCardNumber.Create("5555-6666-7777-8888");
        var items = new List<OrderItem>
        {
            OrderItem.Create("33333", "Bluetooth Speaker", 1, 79.99m)
        };
        
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items, null);

        // Act
        await _repository.AddAsync(order);

        // Assert
        var savedOrder = await _context.Orders.FirstOrDefaultAsync();
        savedOrder.Should().NotBeNull();
        savedOrder!.Notes.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrderNumberAsync_MultipleOrders_ShouldReturnCorrectOrder()
    {
        // Arrange
        var items1 = new List<OrderItem> { OrderItem.Create("11111", "Product 1", 1, 100.00m) };
        var items2 = new List<OrderItem> { OrderItem.Create("22222", "Product 2", 2, 100.00m) };
        var items3 = new List<OrderItem> { OrderItem.Create("33333", "Product 3", 3, 100.00m) };

        var order1 = Order.Create("user1@example.com", InvoiceAddress.Create("Address 1"), CreditCardNumber.Create("1111-1111-1111-1111"), items1);
        var order2 = Order.Create("user2@example.com", InvoiceAddress.Create("Address 2"), CreditCardNumber.Create("2222-2222-2222-2222"), items2);
        var order3 = Order.Create("user3@example.com", InvoiceAddress.Create("Address 3"), CreditCardNumber.Create("3333-3333-3333-3333"), items3);

        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _repository.AddAsync(order3);

        var targetOrderNumber = order2.OrderNumber;

        // Act
        var result = await _repository.GetByOrderNumberAsync(targetOrderNumber);

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Value.Should().Be(targetOrderNumber.Value);
        result.InvoiceEmailAddress.Should().Be("user2@example.com");
        result.TotalAmount.Should().Be(200.00m);
    }

    [Fact]
    public async Task Repository_ShouldPersistOrderStatusCorrectly()
    {
        // Arrange
        var invoiceEmailAddress = "saritas@example.com";
        var invoiceAddress = InvoiceAddress.Create("Status Test Address");
        var creditCardNumber = CreditCardNumber.Create("9999-8888-7777-6666");
        var items = new List<OrderItem>
        {
            OrderItem.Create("44444", "Test Product", 1, 150.00m)
        };
        
        var order = Order.Create(invoiceEmailAddress, invoiceAddress, creditCardNumber, items);

        // Act
        await _repository.AddAsync(order);
        var savedOrder = await _repository.GetByOrderNumberAsync(order.OrderNumber);

        // Assert
        savedOrder.Should().NotBeNull();
        savedOrder!.Status.Should().Be(OrderStatus.Pending);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
