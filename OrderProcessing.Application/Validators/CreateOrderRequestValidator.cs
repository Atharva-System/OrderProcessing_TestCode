using FluentValidation;
using OrderProcessing.Application.DTOs;

namespace OrderProcessing.Application.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.InvoiceEmailAddress)
            .NotEmpty()
            .WithMessage("Invoice email address is required. Please provide a valid email address.")
            .EmailAddress()
            .WithMessage("Please provide a valid email address format (e.g., customer@example.com).");

        RuleFor(x => x.InvoiceAddress)
            .NotEmpty()
            .WithMessage("Invoice address is required. Please provide a complete address.")
            .MinimumLength(10)
            .WithMessage("Invoice address must be at least 10 characters long.")
            .MaximumLength(500)
            .WithMessage("Invoice address cannot exceed 500 characters.");

        RuleFor(x => x.InvoiceCreditCardNumber)
            .NotEmpty()
            .WithMessage("Credit card number is required. Please provide a valid credit card number.")
            .Must(BeValidCreditCardFormat)
            .WithMessage("Credit card number must be in the format XXXX-XXXX-XXXX-XXXX and contain only digits and hyphens.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one product item is required to create an order.")
            .Must(items => items.Count > 0)
            .WithMessage("Order must contain at least one product item.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemRequestValidator());
    }

    private static bool BeValidCreditCardFormat(string creditCardNumber)
    {
        if (string.IsNullOrWhiteSpace(creditCardNumber))
            return false;

        // Remove spaces and hyphens for validation
        var cleaned = creditCardNumber.Replace("-", "").Replace(" ", "");
        
        // Check if it contains only digits and is between 13-19 characters
        return cleaned.All(char.IsDigit) && cleaned.Length >= 13 && cleaned.Length <= 19;
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required. Please provide a valid product identifier.")
            .Length(1, 50)
            .WithMessage("Product ID must be between 1 and 50 characters long.");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required. Please provide a descriptive product name.")
            .Length(1, 200)
            .WithMessage("Product name must be between 1 and 200 characters long.");

        RuleFor(x => x.ProductAmount)
            .GreaterThan(0)
            .WithMessage("Product amount must be greater than zero. Please specify how many items you want to order.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Product amount cannot exceed 1000 items per product.");

        RuleFor(x => x.ProductPrice)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero. Please provide a valid price.")
            .LessThanOrEqualTo(999999.99m)
            .WithMessage("Product price cannot exceed $999,999.99.");
    }
}
