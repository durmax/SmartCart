namespace SmartCart.Domain.Entities;

public class ReceiptItem
{
    public Guid Id { get; set; }

    public Guid ReceiptId { get; set; }

    public Guid? ProductId { get; set; }

    public string OriginalText { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public decimal? Confidence { get; set; }

    public Receipt Receipt { get; set; }

    public Product? Product { get; set; }
}
