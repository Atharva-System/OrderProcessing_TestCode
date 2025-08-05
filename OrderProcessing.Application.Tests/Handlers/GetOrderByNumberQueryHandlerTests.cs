using FluentAssertions;
using Moq;
using OrderProcessing.Application.Handlers;
using OrderProcessing.Application.Queries;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Interfaces;
using OrderProcessing.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderProcessing.Application.Tests.Handlers;

public class GetOrderByNumberQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrderByNumberQueryHandler _handler;

    public GetOrderByNumberQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetOrderByNumberQueryHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingOrderNumber_ShouldReturnOrderResponse()
    {
        // Arrange
        var orderNumberValue = "ORD-12345";

        var orderItems = new List<OrderItem>
        {
            OrderItem.Create("Prod1", "Test Product", 2, 99.99m)
        };

        var order = Order.Create(
            "pooja@example.com",
            InvoiceAddress.Create("100 Test Street"),
            CreditCardNumber.Create("1234-5678-9012-3456"),
            orderItems
        );

        _orderRepositoryMock
            .Setup(r => r.GetByOrderNumberAsync(It.Is<OrderNumber>(on => on.Value == orderNumberValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetOrderByNumberQuery(orderNumberValue);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be(order.OrderNumber.Value);
        result.InvoiceEmailAddress.Should().Be("pooja@example.com");
        result.InvoiceAddress.Should().Be("100 Test Street");
        result.InvoiceCreditCardNumber.Should().Be("1234-5678-9012-3456");
        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.ProductId.Should().Be("Prod1");
        item.ProductName.Should().Be("Test Product");
        item.ProductAmount.Should().Be(2);
        item.ProductPrice.Should().Be(99.99m);
    }

    [Fact]
    public async Task Handle_NonExistingOrderNumber_ShouldReturnNull()
    {
        // Arrange
        var orderNumberValue = "ORD-DOESNOTEXIST";

        _orderRepositoryMock
            .Setup(r => r.GetByOrderNumberAsync(It.Is<OrderNumber>(on => on.Value == orderNumberValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var query = new GetOrderByNumberQuery(orderNumberValue);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
