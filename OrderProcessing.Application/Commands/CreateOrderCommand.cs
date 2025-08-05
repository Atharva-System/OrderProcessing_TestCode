using MediatR;
using OrderProcessing.Application.DTOs;

namespace OrderProcessing.Application.Commands;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderResponse>;
