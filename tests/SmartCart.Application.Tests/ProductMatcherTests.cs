using SmartCart.Application.Services;
using SmartCart.Domain.Entities;

namespace SmartCart.Application.Tests;

public sealed class ProductMatcherTests
{
    [Fact]
    public void Match_ExactEan_ReturnsConfirmedProduct()
    {
        var product = NewProduct("Milk", ean: "4006381333931");

        var result = ProductMatcher.Match("Another label", [product], ean: "4006381333931");

        Assert.Equal(ProductMatchType.ExactEan, result.Type);
        Assert.Same(product, result.ConfirmedProduct);
    }

    [Fact]
    public void Match_VerifiedRetailerAlias_ReturnsConfirmedProduct()
    {
        var product = NewProduct("Long-life milk 1L");
        product.Aliases.Add(new ProductAlias
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Alias = "H-Milch 1 Liter",
            Retailer = "Lidl",
            Product = product
        });

        var result = ProductMatcher.Match("H Milch 1000ml", [product], retailer: "Lidl");

        Assert.Equal(ProductMatchType.VerifiedAlias, result.Type);
        Assert.Same(product, result.ConfirmedProduct);
    }

    [Fact]
    public void Match_NormalizedNameAndEquivalentPackage_ReturnsConfirmedProduct()
    {
        var product = NewProduct("Vollmilch 1L");

        var result = ProductMatcher.Match("Vollmilch 1,0 Liter", [product]);

        Assert.Equal(ProductMatchType.NormalizedName, result.Type);
        Assert.Same(product, result.ConfirmedProduct);
    }

    [Fact]
    public void Match_DifferentPackageSize_DoesNotMatchAutomatically()
    {
        var product = NewProduct("Kaffee 500 g", brand: "Jacobs");

        var result = ProductMatcher.Match("Kaffee 1 kg", [product], brand: "Jacobs");

        Assert.Equal(ProductMatchType.None, result.Type);
        Assert.Null(result.ConfirmedProduct);
    }

    [Fact]
    public void Match_DifferentKnownBrand_DoesNotMatch()
    {
        var product = NewProduct("Krönung Kaffee 500 g", brand: "Jacobs");

        var result = ProductMatcher.Match("Krönung Kaffee 500 g", [product], brand: "Melitta");

        Assert.Equal(ProductMatchType.None, result.Type);
    }

    [Fact]
    public void Match_SimilarNameWithCompatibleCharacteristics_RequiresUserReview()
    {
        var product = NewProduct("Jacobs Krönung Kaffee 500 g", brand: "Jacobs");

        var result = ProductMatcher.Match("Jacobs Krönung gemahlener Kaffee 0,5 kg", [product], brand: "Jacobs");

        Assert.Equal(ProductMatchType.ReviewRequired, result.Type);
        Assert.Contains(product, result.Candidates);
        Assert.Null(result.ConfirmedProduct);
    }

    [Fact]
    public void Match_UnknownProduct_ReturnsNoCandidates()
    {
        var result = ProductMatcher.Match("Unrelated cereal 375 g", [NewProduct("Butter 250 g")]);

        Assert.Equal(ProductMatchType.None, result.Type);
        Assert.Empty(result.Candidates);
    }

    private static Product NewProduct(string name, string? brand = null, string? ean = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        NormalizedName = ProductMatcher.Normalize(name),
        Brand = brand,
        Ean = ean
    };
}
