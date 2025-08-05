using MediatR;
using OrderProcessing.Application.DTOs;

namespace OrderProcessing.Application.Queries;

public record GetOrderByNumberQuery(string OrderNumber) : IRequest<OrderResponse?>;
