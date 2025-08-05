using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Infrastructure.Data;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using OrderProcessing.Application.DTOs;

namespace OrderProcessing.API.IntegrationTests;

public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderProcessingDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }
                
                var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(OrderProcessingDbContext));
                if (dbContextServiceDescriptor != null)
                {
                    services.Remove(dbContextServiceDescriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<OrderProcessingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });

            builder.UseEnvironment("Testing");
        });

        _client = _factory.CreateClient();
    }

    private async Task CleanupDbAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ShouldReturnCreatedOrder()
    {
        // Arrange
        await CleanupDbAsync();
        
        var request = new CreateOrderRequest
        {
            InvoiceEmailAddress = "rajesh@example.com",
            InvoiceAddress = "123 Sample Street, 90402 Berlin",
            InvoiceCreditCardNumber = "1234-5678-9101-1121",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = "12345", ProductName = "Gaming Laptop", ProductAmount = 2, ProductPrice = 1499.99m },
                new() { ProductId = "67890", ProductName = "Wireless Mouse", ProductAmount = 1, ProductPrice = 29.99m }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<OrderResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        createdOrder.Should().NotBeNull();
        createdOrder!.InvoiceEmailAddress.Should().Be("rajesh@example.com");
        createdOrder.InvoiceAddress.Should().Be("123 Sample Street, 90402 Berlin");
        createdOrder.InvoiceCreditCardNumber.Should().Be("1234-5678-9101-1121");
        createdOrder.OrderNumber.Should().NotBeNullOrEmpty();
        createdOrder.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderByNumber_ExistingOrder_ShouldReturnOrder()
    {
        // Arrange - First create an order
        await CleanupDbAsync();
        
        var createRequest = new CreateOrderRequest
        {
            InvoiceEmailAddress = "ramesh@example.com",
            InvoiceAddress = "456 Main Street, 10001 New York",
            InvoiceCreditCardNumber = "9876-5432-1098-7654",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = "11111", ProductName = "Mechanical Keyboard", ProductAmount = 1, ProductPrice = 149.99m }
            }
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync("/api/orders", createContent);
        var createResponseContent = await createResponse.Content.ReadAsStringAsync();
        var createdOrder = JsonSerializer.Deserialize<OrderResponse>(createResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.OrderNumber}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var retrievedOrder = JsonSerializer.Deserialize<OrderResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        retrievedOrder.Should().NotBeNull();
        retrievedOrder!.OrderNumber.Should().Be(createdOrder.OrderNumber);
        retrievedOrder.InvoiceEmailAddress.Should().Be("ramesh@example.com");
    }

    [Fact]
    public async Task GetOrderByNumber_NonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        await CleanupDbAsync();
        
        // Act
        var response = await _client.GetAsync("/api/orders/ORD-NONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await CleanupDbAsync();
        
        var request = new CreateOrderRequest
        {
            InvoiceEmailAddress = "invalid-email", // Invalid email format
            InvoiceAddress = "", // Invalid - empty address
            InvoiceCreditCardNumber = "123", // Invalid - too short
            Items = new List<CreateOrderItemRequest>() // Invalid - no items
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
