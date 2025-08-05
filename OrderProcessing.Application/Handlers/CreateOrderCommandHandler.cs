using MediatR;
using OrderProcessing.Application.Commands;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Interfaces;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Create value objects
        var invoiceAddress = InvoiceAddress.Create(request.Request.InvoiceAddress);
        var creditCardNumber = CreditCardNumber.Create(request.Request.InvoiceCreditCardNumber);
        
        // Create order items
        var orderItems = request.Request.Items.Select(item => 
            OrderItem.Create(item.ProductId, item.ProductName, item.ProductAmount, item.ProductPrice)
        ).ToList();

        // Create the order
        var order = Order.Create(
            request.Request.InvoiceEmailAddress,
            invoiceAddress,
            creditCardNumber,
            orderItems
        );

        var createdOrder = await _orderRepository.AddAsync(order, cancellationToken);

        return MapToResponse(createdOrder);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            OrderNumber = order.OrderNumber.Value,
            InvoiceEmailAddress = order.InvoiceEmailAddress,
            InvoiceAddress = order.InvoiceAddress.Value,
            InvoiceCreditCardNumber = order.InvoiceCreditCardNumber.Value,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductAmount = item.ProductAmount,
                ProductPrice = item.ProductPrice
            }).ToList()
        };
    }
}
