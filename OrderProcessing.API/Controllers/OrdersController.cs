using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Application.Commands;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Queries;
using System.Text.Json;

namespace OrderProcessing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<CreateOrderRequest> _createOrderValidator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IMediator mediator,
        IValidator<CreateOrderRequest> createOrderValidator,
        ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _createOrderValidator = createOrderValidator;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint for diagnostics
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        try
        {
            _logger.LogInformation("Health check requested");
            return Ok(new 
            { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                machineName = Environment.MachineName,
                processId = Environment.ProcessId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new order
    /// </summary>
    /// <param name="request">The order creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created order</returns>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "CreateOrder"
        });

        try
        {
            _logger.LogInformation("=== CREATE ORDER REQUEST STARTED === CorrelationId: {CorrelationId}", correlationId);
            _logger.LogDebug("Request payload: {@Request}", request);
            
            _logger.LogInformation("Creating new order for email: {InvoiceEmailAddress}", request?.InvoiceEmailAddress ?? "null");

            // Null check
            if (request == null)
            {
                _logger.LogWarning("Request is null");
                var nullErrorResponse = new ErrorResponse
                {
                    Title = "Invalid Request",
                    Message = "Request body cannot be null or empty.",
                    StatusCode = 400,
                    TraceId = HttpContext.TraceIdentifier
                };
                return BadRequest(nullErrorResponse);
            }

            // Validate the request
            _logger.LogDebug("Starting validation for CorrelationId: {CorrelationId}", correlationId);
            var validationResult = await _createOrderValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for CorrelationId: {CorrelationId}. Errors: {@ValidationErrors}", 
                    correlationId, validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
                
                var errors = validationResult.Errors.Select(e => new ValidationError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage,
                    AttemptedValue = e.AttemptedValue
                }).ToList();

                var errorResponse = new ErrorResponse
                {
                    Title = "Validation Failed",
                    Message = "One or more validation errors occurred. Please check your input data.",
                    StatusCode = 400,
                    Errors = errors,
                    TraceId = HttpContext.TraceIdentifier
                };

                return BadRequest(errorResponse);
            }

            _logger.LogDebug("Validation passed for CorrelationId: {CorrelationId}, creating command...", correlationId);
            var command = new CreateOrderCommand(request);
            
            _logger.LogDebug("Sending command via MediatR for CorrelationId: {CorrelationId}", correlationId);
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Order created successfully with order number: {OrderNumber}, CorrelationId: {CorrelationId}", 
                result.OrderNumber, correlationId);
            _logger.LogDebug("=== CREATE ORDER REQUEST COMPLETED === CorrelationId: {CorrelationId}", correlationId);

            return CreatedAtAction(
                nameof(GetOrderByNumber),
                new { orderNumber = result.OrderNumber },
                result);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "JSON parsing error for CorrelationId: {CorrelationId} - {Message}", correlationId, jsonEx.Message);
            var errorResponse = new ErrorResponse
            {
                Title = "Invalid JSON Format",
                Message = "The request contains invalid JSON data. Please check the format of your input.",
                StatusCode = 400,
                Errors = ParseJsonError(jsonEx.Message),
                TraceId = HttpContext.TraceIdentifier
            };
            return BadRequest(errorResponse);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "ArgumentException for CorrelationId: {CorrelationId} - {Message}", correlationId, ex.Message);
            var errorResponse = new ErrorResponse
            {
                Title = "Invalid Input",
                Message = ex.Message,
                StatusCode = 400,
                TraceId = HttpContext.TraceIdentifier
            };
            return BadRequest(errorResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "InvalidOperationException for CorrelationId: {CorrelationId} - {Message}", correlationId, ex.Message);
            var errorResponse = new ErrorResponse
            {
                Title = "Invalid Operation",
                Message = ex.Message,
                StatusCode = 400,
                TraceId = HttpContext.TraceIdentifier
            };
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for CorrelationId: {CorrelationId} - Type: {ExceptionType}, Message: {Message}", 
                correlationId, ex.GetType().Name, ex.Message);
            
            var errorResponse = new ErrorResponse
            {
                Title = "Internal Server Error",
                Message = "An unexpected error occurred. Please try again later.",
                StatusCode = 500,
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Gets an order by its order number
    /// </summary>
    /// <param name="orderNumber">The order number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The order if found</returns>
    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<OrderResponse>> GetOrderByNumber(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "GetOrderByNumber",
            ["OrderNumber"] = orderNumber
        });

        try
        {
            _logger.LogInformation("Retrieving order with number: {OrderNumber}, CorrelationId: {CorrelationId}", orderNumber, correlationId);

            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                _logger.LogWarning("Order number is null or empty for CorrelationId: {CorrelationId}", correlationId);
                var errorResponse = new ErrorResponse
                {
                    Title = "Invalid Order Number",
                    Message = "Order number cannot be null or empty.",
                    StatusCode = 400,
                    TraceId = HttpContext.TraceIdentifier
                };
                return BadRequest(errorResponse);
            }

            var query = new GetOrderByNumberQuery(orderNumber);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Order not found with number: {OrderNumber}, CorrelationId: {CorrelationId}", orderNumber, correlationId);
                var errorResponse = new ErrorResponse
                {
                    Title = "Order Not Found",
                    Message = $"No order was found with the number '{orderNumber}'. Please check the order number and try again.",
                    StatusCode = 404,
                    TraceId = HttpContext.TraceIdentifier
                };
                return NotFound(errorResponse);
            }

            _logger.LogInformation("Order retrieved successfully: {OrderNumber}, CorrelationId: {CorrelationId}", orderNumber, correlationId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid order number format: {OrderNumber}, CorrelationId: {CorrelationId}, Error: {Message}", 
                orderNumber, correlationId, ex.Message);
            var errorResponse = new ErrorResponse
            {
                Title = "Invalid Order Number",
                Message = ex.Message,
                StatusCode = 400,
                TraceId = HttpContext.TraceIdentifier
            };
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving order: {OrderNumber}, CorrelationId: {CorrelationId}", 
                orderNumber, correlationId);
            var errorResponse = new ErrorResponse
            {
                Title = "Internal Server Error",
                Message = "An unexpected error occurred while retrieving the order. Please try again later.",
                StatusCode = 500,
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    private static List<ValidationError> ParseJsonError(string jsonErrorMessage)
    {
        var errors = new List<ValidationError>();

        // Parse common JSON errors and convert to user-friendly messages
        if (jsonErrorMessage.Contains("could not be converted to System.String"))
        {
            if (jsonErrorMessage.Contains("productId"))
            {
                errors.Add(new ValidationError
                {
                    Field = "items[].productId",
                    Message = "Product ID must be text enclosed in quotes. Example: \"12345\" instead of 12345"
                });
            }
            if (jsonErrorMessage.Contains("productName"))
            {
                errors.Add(new ValidationError
                {
                    Field = "items[].productName",
                    Message = "Product name must be text enclosed in quotes. Example: \"Gaming Laptop\" instead of Gaming Laptop"
                });
            }
        }

        if (jsonErrorMessage.Contains("could not be converted to System.Int32"))
        {
            if (jsonErrorMessage.Contains("productAmount"))
            {
                errors.Add(new ValidationError
                {
                    Field = "items[].productAmount",
                    Message = "Product amount must be a valid whole number without quotes. Example: 2 instead of \"2\""
                });
            }
        }

        if (jsonErrorMessage.Contains("could not be converted to System.Decimal"))
        {
            if (jsonErrorMessage.Contains("productPrice"))
            {
                errors.Add(new ValidationError
                {
                    Field = "items[].productPrice",
                    Message = "Product price must be a valid decimal number without quotes. Example: 1499.99 instead of \"1499.99\""
                });
            }
        }

        if (jsonErrorMessage.Contains("'!' is an invalid"))
        {
            errors.Add(new ValidationError
            {
                Field = "json",
                Message = "Invalid character '!' found. Remove any special characters that are not part of valid JSON."
            });
        }

        // If no specific field errors found, add a general JSON format error
        if (!errors.Any())
        {
            errors.Add(new ValidationError
            {
                Field = "request",
                Message = "Invalid JSON format. Please ensure: text values are in quotes, numbers are not in quotes, and all syntax is correct."
            });
        }

        return errors;
    }
}
