using MediatR;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Queries;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Interfaces;
using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Application.Handlers;

public class GetOrderByNumberQueryHandler : IRequestHandler<GetOrderByNumberQuery, OrderResponse?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByNumberQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse?> Handle(GetOrderByNumberQuery request, CancellationToken cancellationToken)
    {
        var orderNumber = OrderNumber.Create(request.OrderNumber);
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);

        return order == null ? null : MapToResponse(order);
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
