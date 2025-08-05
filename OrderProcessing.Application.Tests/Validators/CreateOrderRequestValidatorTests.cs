using FluentAssertions;
using FluentValidation.TestHelper;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Validators;
using Xunit;
using System.Collections.Generic;

namespace OrderProcessing.Application.Tests.Validators;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator;

    public CreateOrderRequestValidatorTests()
    {
        _validator = new CreateOrderRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        var model = new CreateOrderRequest { InvoiceEmailAddress = "invalidemail" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.InvoiceEmailAddress);
    }

    [Fact]
    public void Should_Have_Error_When_Address_Is_Too_Short()
    {
        var model = new CreateOrderRequest { InvoiceAddress = "Short" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.InvoiceAddress);
    }

    [Fact]
    public void Should_Have_Error_When_CreditCard_Format_Is_Invalid()
    {
        var model = new CreateOrderRequest { InvoiceCreditCardNumber = "1234567890" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.InvoiceCreditCardNumber);
    }

    [Fact]
    public void Should_Have_Error_When_Items_Is_Empty()
    {
        var model = new CreateOrderRequest { Items = new List<CreateOrderItemRequest>() };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Request()
    {
        var model = new CreateOrderRequest
        {
            InvoiceEmailAddress = "visal@example.com",
            InvoiceAddress = "123 Main Street, Some City",
            InvoiceCreditCardNumber = "1234-5678-9012-3456",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = "P01",
                    ProductName = "Product 1",
                    ProductAmount = 1,
                    ProductPrice = 99.99m
                }
            }
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
