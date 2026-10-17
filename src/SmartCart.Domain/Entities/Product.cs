namespace SmartCart.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? NormalizedName { get; set; }

    public string? Ean { get; set; }

    public ICollection<ProductAlias> Aliases { get; set; } = new List<ProductAlias>();
}
