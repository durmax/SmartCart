namespace SmartCart.Application.Receipts;

public sealed class ReceiptDetailsResult
{
    public Guid Id { get; init; }
    public string Retailer { get; init; } = string.Empty;
    public string? ReceiptNumber { get; init; }
    public DateTimeOffset? PurchaseDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? StoreNumber { get; init; }
    public string? ImagePath { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? RawOcrText { get; init; }
    public List<ReceiptItemDetailsResult> Items { get; init; } = [];
}

public sealed class ReceiptItemDetailsResult
{
    public Guid Id { get; init; }
    public string OriginalText { get; init; } = string.Empty;
    public string? ParsedName { get; init; }
    public decimal? Quantity { get; init; }
    public string? Unit { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? TotalPrice { get; init; }
    public Guid? ProductId { get; init; }
    public decimal? Confidence { get; init; }
    public List<ReceiptItemAdjustmentDetailsResult> Adjustments { get; init; } = [];
}

public sealed class ReceiptItemAdjustmentDetailsResult
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
