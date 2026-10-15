namespace SmartCart.Domain.Entities;

public class ReceiptItemAdjustment
{
    public Guid Id { get; set; }

    public Guid ReceiptItemId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public ReceiptItem ReceiptItem { get; set; } = null!;
}
