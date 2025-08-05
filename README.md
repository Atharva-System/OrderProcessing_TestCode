# Order Processing Service

This is a .NET 9 microservice I built to handle order processing using Domain-Driven Design principles. The main goal was to create a clean, well-tested API that can handle order creation and retrieval with proper validation and error handling.

## What it does

The service provides two main endpoints:
- `POST /api/orders` - Create a new order
- `GET /api/orders/{orderNumber}` - Get an existing order

## Why I chose this architecture

I went with DDD because order processing has clear business rules and concepts that map well to domain entities. The clean architecture approach keeps everything organized:

```
OrderProcessing.Domain/     # Core business logic
OrderProcessing.Application/ # Use cases and commands  
OrderProcessing.Infrastructure/ # Database and external concerns
OrderProcessing.API/        # Web API endpoints
```

## Key decisions I made

**Domain modeling**: Orders are aggregate roots that contain order items. I used value objects for things like order numbers and credit card info to enforce business rules at the type level.

**CQRS with MediatR**: Commands for writes, queries for reads. Makes the code easier to follow and test.

**Error handling**: Spent time making error messages actually useful instead of throwing technical jargon at API consumers. If someone sends malformed JSON, they get clear guidance on how to fix it.

**Testing**: Unit tests for domain logic, integration tests for the full API. The domain tests were especially important since that's where the business rules live.

## Running it locally

You'll need:
- .NET 9 SDK
- PostgreSQL running somewhere

Steps:
1. Clone this repo
2. Update the connection string in `appsettings.json` if needed
3. Run `dotnet run --project OrderProcessing.API`
4. Hit `https://localhost:7001` for Swagger

The database gets created automatically on first run.

## API examples

**Creating an order:**
```json
POST /api/orders
{
  "items": [
    {
      "productId": "12345",
      "productName": "Gaming Laptop",
      "productAmount": 2,
      "productPrice": 1499.99
    }
  ],
  "invoiceAddress": "123 Sample Street, 90402 Berlin",
  "invoiceEmailAddress": "customer@example.com", 
  "invoiceCreditCardNumber": "1234-5678-9101-1121"
}
```

**Getting an order:**
```
GET /api/orders/ORD-20250105-123456
```

## What I learned

The hardest part was getting the Entity Framework configuration right for the value objects. Had to use owned entities and custom configurations to make it work properly.

Error handling was another challenge - balancing helpful error messages without exposing internal implementation details.

## Testing

Run tests with `dotnet test`. I tried to cover:
- Domain logic (business rules, value object validation)
- Application handlers 
- Repository operations
- Full API integration tests

## Tech stack

- .NET 9 / C# 13
- ASP.NET Core Web API
- Entity Framework Core + PostgreSQL
- MediatR for CQRS
- FluentValidation 
- Serilog for logging
- XUnit for testing
