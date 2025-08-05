using FluentAssertions;
using FluentValidation.TestHelper;
using OrderProcessing.Application.DTOs;
using OrderProcessing.Application.Validators;
using Xunit;

namespace OrderProcessing.Application.Tests.Validators;

public class CreateOrderItemRequestValidatorTests
{
    private readonly CreateOrderItemRequestValidator _validator;

    public CreateOrderItemRequestValidatorTests()
    {
        _validator = new CreateOrderItemRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_ProductId_Is_Empty()
    {
        var model = new CreateOrderItemRequest { ProductId = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void Should_Have_Error_When_ProductName_Is_Empty()
    {
        var model = new CreateOrderItemRequest { ProductName = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void Should_Have_Error_When_ProductAmount_Is_Zero()
    {
        var model = new CreateOrderItemRequest { ProductAmount = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProductAmount);
    }

    [Fact]
    public void Should_Have_Error_When_ProductPrice_Is_Zero()
    {
        var model = new CreateOrderItemRequest { ProductPrice = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProductPrice);
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Item()
    {
        var model = new CreateOrderItemRequest
        {
            ProductId = "P001",
            ProductName = "Valid Product",
            ProductAmount = 2,
            ProductPrice = 150.00m
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
