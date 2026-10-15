namespace SmartCart.Application.Receipts;

public sealed class ParsedReceiptItem
{
    public string OriginalText { get; init; } = string.Empty;

    public string? ParsedName { get; init; }

    public decimal? Quantity { get; init; }

    public string? Unit { get; init; }

    public decimal? UnitPrice { get; init; }

    public decimal? TotalPrice { get; init; }

    public List<ParsedReceiptItemAdjustment> Adjustments { get; init; } = [];
}
