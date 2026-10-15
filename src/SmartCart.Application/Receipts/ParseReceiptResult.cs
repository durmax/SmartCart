namespace SmartCart.Application.Receipts;

public sealed class ParseReceiptResult
{
    public Guid ReceiptId { get; init; }

    public string? ReceiptNumber { get; init; }

    public DateTimeOffset? PurchaseDate { get; init; }

    public decimal? TotalAmount { get; init; }

    public List<ParsedReceiptItem> Items { get; init; } = [];
}
