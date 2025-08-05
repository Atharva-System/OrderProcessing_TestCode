using FluentAssertions;
using Moq;
using OrderProcessing.Application.Commands;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Handlers;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Interfaces;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Application.Tests.Handlers;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new CreateOrderCommandHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var requestDto = new CreateOrderRequest
        {
            InvoiceEmailAddress = "suresh@example.com",
            InvoiceAddress = "123 Test Street",
            InvoiceCreditCardNumber = "1234-5678-9012-3456",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = "P001", ProductName = "Product 1", ProductAmount = 2, ProductPrice = 100 },
                new() { ProductId = "P002", ProductName = "Product 2", ProductAmount = 1, ProductPrice = 50 }
            }
        };

        var expectedOrder = Order.Create(
            requestDto.InvoiceEmailAddress,
            InvoiceAddress.Create(requestDto.InvoiceAddress),
            CreditCardNumber.Create(requestDto.InvoiceCreditCardNumber),
            requestDto.Items.Select(i => OrderItem.Create(i.ProductId, i.ProductName, i.ProductAmount, i.ProductPrice)).ToList()
        );

        _orderRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        var command = new CreateOrderCommand(requestDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.InvoiceEmailAddress.Should().Be("suresh@example.com");
        result.InvoiceAddress.Should().Be("123 Test Street");
        result.InvoiceCreditCardNumber.Should().Be("1234-5678-9012-3456");
        result.Items.Should().HaveCount(2);
        result.Items[0].ProductName.Should().Be("Product 1");
        result.Items[0].ProductAmount.Should().Be(2);
        result.Items[0].ProductPrice.Should().Be(100);
    }
    [Fact]
    public async Task Handle_RequestWithNoItems_ShouldThrowArgumentException()
    {
        // Arrange
        var requestDto = new CreateOrderRequest
        {
            InvoiceEmailAddress = "rajan@example.com",
            InvoiceAddress = "No Items Lane",
            InvoiceCreditCardNumber = "0000-0000-0000-0000",
            Items = new List<CreateOrderItemRequest>()
        };

        var command = new CreateOrderCommand(requestDto);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ArgumentException>();
        exception.Which.Message.Should().StartWith("Order must have at least one item");
        exception.Which.ParamName.Should().Be("items");

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }



}
