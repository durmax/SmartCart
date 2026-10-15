namespace SmartCart.Application.Receipts;

public sealed class ParsedReceiptItemAdjustment
{
    public string Description { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}
