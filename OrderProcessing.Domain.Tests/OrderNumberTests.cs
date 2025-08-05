using OrderProcessing.Domain.ValueObjects;
using Xunit;

namespace OrderProcessing.Domain.Tests;

public class OrderNumberTests
{
    [Fact]
    public void Create_ValidOrderNumber_ShouldCreateOrderNumber()
    {
        // Arrange
        var value = "ORD-12345";

        // Act
        var orderNumber = OrderNumber.Create(value);

        // Assert
        Assert.Equal(value, orderNumber.Value);
        Assert.Equal(value, orderNumber.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("AB")]
    [InlineData("ThisOrderNumberIsTooLongForTheValidation")]
    public void Create_InvalidOrderNumber_ShouldThrowArgumentException(string invalidValue)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => OrderNumber.Create(invalidValue));
    }

    [Fact]
    public void Generate_ShouldCreateValidOrderNumber()
    {
        // Act
        var orderNumber = OrderNumber.Generate();

        // Assert
        Assert.NotNull(orderNumber.Value);
        Assert.StartsWith("ORD-", orderNumber.Value);
        Assert.True(orderNumber.Value.Length > 10);
    }

    [Fact]
    public void ImplicitConversion_ShouldConvertToString()
    {
        // Arrange
        var orderNumber = OrderNumber.Create("ORD-12345");

        // Act
        string value = orderNumber;

        // Assert
        Assert.Equal("ORD-12345", value);
    }
}
