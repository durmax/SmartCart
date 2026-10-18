namespace SmartCart.Web.Services;

public sealed record StockUpSuggestion(decimal AdditionalQuantity, decimal EstimatedSavings);

public static class StockUpCalculator
{
    public static StockUpSuggestion? Calculate(
        decimal plannedQuantity,
        decimal maximumTotalQuantity,
        decimal regularPrice,
        decimal offerPrice,
        bool? isPerishable)
    {
        if (isPerishable is not false || plannedQuantity <= 0 || maximumTotalQuantity <= plannedQuantity ||
            regularPrice <= offerPrice || offerPrice <= 0)
        {
            return null;
        }

        var additionalQuantity = Math.Min(plannedQuantity, maximumTotalQuantity - plannedQuantity);
        return additionalQuantity <= 0
            ? null
            : new StockUpSuggestion(additionalQuantity, additionalQuantity * (regularPrice - offerPrice));
    }
}
