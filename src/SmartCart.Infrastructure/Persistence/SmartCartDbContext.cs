using Microsoft.EntityFrameworkCore;
using SmartCart.Domain.Entities;

namespace SmartCart.Infrastructure.Persistence;

public class SmartCartDbContext(DbContextOptions<SmartCartDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();

    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasMany(product => product.Aliases)
            .WithOne(alias => alias.Product)
            .HasForeignKey(alias => alias.ProductId);

        modelBuilder.Entity<Offer>()
            .Property(offer => offer.RegularPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Offer>()
            .Property(offer => offer.OfferPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Product>()
            .HasIndex(product => product.NormalizedName);

        modelBuilder.Entity<Product>()
            .HasIndex(product => product.Ean);

        modelBuilder.Entity<ProductAlias>()
            .HasIndex(alias => new { alias.Retailer, alias.Alias });

    }
}
