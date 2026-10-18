using SmartCart.Web.Services;

namespace SmartCart.Web.Tests;

public sealed class StockUpCalculatorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(null)]
    public void Calculate_DisablesPerishableOrUnknownProducts(bool? isPerishable)
    {
        var suggestion = StockUpCalculator.Calculate(1, 3, 2, 1, isPerishable);

        Assert.Null(suggestion);
    }

    [Fact]
    public void Calculate_CapsAdditionalQuantityAtNeedAndUserLimit()
    {
        var suggestion = StockUpCalculator.Calculate(2, 5, 3, 2, false);

        Assert.Equal(new StockUpSuggestion(2, 2), suggestion);
    }

    [Fact]
    public void Calculate_ReturnsNoSuggestionWithoutSavingOrAvailableQuantity()
    {
        Assert.Null(StockUpCalculator.Calculate(2, 5, 2, 2, false));
        Assert.Null(StockUpCalculator.Calculate(2, 2, 3, 2, false));
    }
}
