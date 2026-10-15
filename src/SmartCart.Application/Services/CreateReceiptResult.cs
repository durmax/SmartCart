namespace SmartCart.Application.Services;

public sealed class CreateReceiptResult
{
    public Guid Id { get; init; }

    public string Retailer { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
