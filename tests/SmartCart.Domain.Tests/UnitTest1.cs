using SmartCart.Domain.Entities;

namespace SmartCart.Domain.Tests;

public class ReceiptItemTests
{
    [Fact]
    public void Adjustments_CanContainMultipleNegativeAmounts()
    {
        var receiptItem = new ReceiptItem
        {
            Adjustments =
            [
                new ReceiptItemAdjustment { Description = "Preisvorteil", Amount = -0.10m },
                new ReceiptItemAdjustment { Description = "Lidl Plus Rabatt", Amount = -0.20m }
            ]
        };

        Assert.Equal(2, receiptItem.Adjustments.Count);
        Assert.Collection(
            receiptItem.Adjustments,
            adjustment => Assert.Equal(-0.10m, adjustment.Amount),
            adjustment => Assert.Equal(-0.20m, adjustment.Amount));
    }
}
