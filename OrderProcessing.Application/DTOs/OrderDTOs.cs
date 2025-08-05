using OrderProcessing.Domain.ValueObjects;

namespace OrderProcessing.Application.DTOs;

public record ErrorResponse
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public List<ValidationError> Errors { get; init; } = new();
    public string TraceId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? RequestPath { get; init; }
    public string? RequestMethod { get; init; }
}

public record ValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public object? AttemptedValue { get; init; }
    public string? ErrorCode { get; init; }
}

public record CreateOrderRequest
{
    public List<CreateOrderItemRequest> Items { get; init; } = new();
    public string InvoiceAddress { get; init; } = string.Empty;
    public string InvoiceEmailAddress { get; init; } = string.Empty;
    public string InvoiceCreditCardNumber { get; init; } = string.Empty;
}

public record CreateOrderItemRequest
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int ProductAmount { get; init; }
    public decimal ProductPrice { get; init; }
}

public record OrderResponse
{
    public string OrderNumber { get; init; } = string.Empty;
    public List<OrderItemResponse> Items { get; init; } = new();
    public string InvoiceAddress { get; init; } = string.Empty;
    public string InvoiceEmailAddress { get; init; } = string.Empty;
    public string InvoiceCreditCardNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record OrderItemResponse
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int ProductAmount { get; init; }
    public decimal ProductPrice { get; init; }
}
