namespace SmartCart.Domain.Entities;

public class Receipt
{
    public Guid Id { get; set; }

    public string Retailer { get; set; } = string.Empty;

    public string? ReceiptNumber { get; set; }

    public DateTimeOffset? PurchaseDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? StoreNumber { get; set; }

    public string? ImagePath { get; set; }

    public string? RawOcrText { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
}
