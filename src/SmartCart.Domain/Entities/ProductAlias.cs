namespace SmartCart.Domain.Entities;

public class ProductAlias
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Alias { get; set; } = string.Empty;

    public string? Retailer { get; set; }

    public Product Product { get; set; }
}
