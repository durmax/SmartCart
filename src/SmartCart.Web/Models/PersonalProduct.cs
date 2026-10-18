using SmartCart.Application.Services;
using SmartCart.Domain.Entities;

namespace SmartCart.Web.Models;

public sealed class PersonalProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string NormalizedName { get; set; } = string.Empty;
    public string? Ean { get; set; }
    public decimal? PackageQuantity { get; set; }
    public string? PackageUnit { get; set; }
    public decimal? UsualQuantity { get; set; }
    public List<PersonalProductAlias> Aliases { get; set; } = [];

    public Product ToDomainProduct()
    {
        var product = new Product
        {
            Id = Id,
            Name = Name,
            Brand = Brand,
            NormalizedName = string.IsNullOrWhiteSpace(NormalizedName) ? ProductMatcher.Normalize(Name) : NormalizedName,
            Ean = Ean
        };
        product.Aliases = Aliases.Select(alias => new ProductAlias
        {
            Id = alias.Id,
            ProductId = Id,
            Alias = alias.Alias,
            Retailer = alias.Retailer,
            Product = product
        }).ToList();
        return product;
    }
}

public sealed class PersonalProductAlias
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Alias { get; set; } = string.Empty;
    public string? Retailer { get; set; }
}
