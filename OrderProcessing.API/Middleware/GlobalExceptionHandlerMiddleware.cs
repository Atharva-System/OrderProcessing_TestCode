using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.DTOs;
using System.Net;
using System.Text.Json;

namespace OrderProcessing.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Request: {Method} {Path} {QueryString}", 
                context.Request.Method, 
                context.Request.Path, 
                context.Request.QueryString);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case JsonException jsonEx:
                _logger.LogWarning("JSON parsing error: {Message} at Path: {Path}", jsonEx.Message, jsonEx.Path);
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = errorResponse with
                {
                    Title = "Invalid JSON Format",
                    Message = "The request contains invalid JSON data. Please check your input format.",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Errors = ParseJsonError(jsonEx.Message)
                };
                break;

            case ArgumentException argEx:
                _logger.LogWarning("Invalid argument: {Message} - Parameter: {ParamName}", argEx.Message, argEx.ParamName);
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = errorResponse with
                {
                    Title = "Invalid Input",
                    Message = argEx.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
                break;

            case InvalidOperationException invOpEx:
                _logger.LogWarning("Invalid operation: {Message}", invOpEx.Message);
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = errorResponse with
                {
                    Title = "Invalid Operation",
                    Message = invOpEx.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
                break;

            case DbUpdateException dbEx:
                _logger.LogError(dbEx, "Database update error: {Message}", dbEx.Message);
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = errorResponse with
                {
                    Title = "Database Error",
                    Message = "A database error occurred while processing your request.",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
                break;

            case TimeoutException timeoutEx:
                _logger.LogError(timeoutEx, "Request timeout: {Message}", timeoutEx.Message);
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse = errorResponse with
                {
                    Title = "Request Timeout",
                    Message = "The request took too long to process. Please try again.",
                    StatusCode = (int)HttpStatusCode.RequestTimeout
                };
                break;

            case UnauthorizedAccessException unauthorizedEx:
                _logger.LogWarning("Unauthorized access attempt: {Message}", unauthorizedEx.Message);
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = errorResponse with
                {
                    Title = "Unauthorized",
                    Message = "You are not authorized to perform this action.",
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };
                break;

            case NotSupportedException notSupportedEx:
                _logger.LogError(notSupportedEx, "Operation not supported: {Message}", notSupportedEx.Message);
                response.StatusCode = (int)HttpStatusCode.NotImplemented;
                errorResponse = errorResponse with
                {
                    Title = "Operation Not Supported",
                    Message = "The requested operation is not supported.",
                    StatusCode = (int)HttpStatusCode.NotImplemented
                };
                break;

            default:
                _logger.LogError(exception, "Unexpected error occurred: {ExceptionType} - {Message} - StackTrace: {StackTrace}", 
                    exception.GetType().Name, 
                    exception.Message, 
                    exception.StackTrace);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                
                // In development, include more details
                var message = _environment.IsDevelopment() 
                    ? $"An unexpected error occurred: {exception.Message}" 
                    : "An unexpected error occurred. Please try again later.";

                errorResponse = errorResponse with
                {
                    Title = "Internal Server Error",
                    Message = message,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };

                // Add stack trace in development mode
                if (_environment.IsDevelopment())
                {
                    errorResponse = errorResponse with
                    {
                        Errors = new List<ValidationError>
                        {
                            new ValidationError
                            {
                                Field = "debug",
                                Message = $"Exception Type: {exception.GetType().Name}",
                                AttemptedValue = exception.StackTrace
                            }
                        }
                    };
                }
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var result = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(result);
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
                    Message = "Product ID must be text enclosed in quotes (e.g., \"12345\")"
                });
            }
            if (jsonErrorMessage.Contains("productName"))
            {
                errors.Add(new ValidationError
                {
                    Field = "items[].productName",
                    Message = "Product name must be text enclosed in quotes (e.g., \"Gaming Laptop\")"
                });
            }
            if (jsonErrorMessage.Contains("invoiceAddress"))
            {
                errors.Add(new ValidationError
                {
                    Field = "invoiceAddress",
                    Message = "Invoice address must be text enclosed in quotes"
                });
            }
            if (jsonErrorMessage.Contains("invoiceEmailAddress"))
            {
                errors.Add(new ValidationError
                {
                    Field = "invoiceEmailAddress",
                    Message = "Invoice email address must be text enclosed in quotes"
                });
            }
            if (jsonErrorMessage.Contains("invoiceCreditCardNumber"))
            {
                errors.Add(new ValidationError
                {
                    Field = "invoiceCreditCardNumber",
                    Message = "Credit card number must be text enclosed in quotes"
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
                    Message = "Product amount must be a valid whole number without quotes (e.g., 2)"
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
                    Message = "Product price must be a valid decimal number without quotes (e.g., 29.99)"
                });
            }
        }

        if (jsonErrorMessage.Contains("'!' is an invalid") || jsonErrorMessage.Contains("Unexpected character"))
        {
            errors.Add(new ValidationError
            {
                Field = "json",
                Message = "Invalid character found. Remove any special characters that are not part of valid JSON."
            });
        }

        if (jsonErrorMessage.Contains("Expected depth to be zero"))
        {
            errors.Add(new ValidationError
            {
                Field = "json",
                Message = "JSON structure is incomplete. Check for missing closing brackets or braces."
            });
        }

        if (jsonErrorMessage.Contains("is an invalid start of a property name"))
        {
            errors.Add(new ValidationError
            {
                Field = "json",
                Message = "Property names must be enclosed in double quotes."
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