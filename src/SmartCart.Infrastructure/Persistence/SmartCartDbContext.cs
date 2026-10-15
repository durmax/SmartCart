using Microsoft.EntityFrameworkCore;
using SmartCart.Domain.Entities;

namespace SmartCart.Infrastructure.Persistence;

public class SmartCartDbContext(DbContextOptions<SmartCartDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();

    public DbSet<ReceiptItemAdjustment> ReceiptItemAdjustments => Set<ReceiptItemAdjustment>();

    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasMany(product => product.Aliases)
            .WithOne(alias => alias.Product)
            .HasForeignKey(alias => alias.ProductId);

        modelBuilder.Entity<Product>()
            .HasMany(product => product.ReceiptItems)
            .WithOne(item => item.Product)
            .HasForeignKey(item => item.ProductId)
            .IsRequired(false);

        modelBuilder.Entity<Receipt>()
            .HasMany(receipt => receipt.Items)
            .WithOne(item => item.Receipt)
            .HasForeignKey(item => item.ReceiptId);

        modelBuilder.Entity<ReceiptItem>()
            .HasMany(item => item.Adjustments)
            .WithOne(adjustment => adjustment.ReceiptItem)
            .HasForeignKey(adjustment => adjustment.ReceiptItemId);

        modelBuilder.Entity<Receipt>()
            .Property(receipt => receipt.TotalAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ReceiptItem>()
            .Property(item => item.Quantity)
            .HasPrecision(10, 3);

        modelBuilder.Entity<ReceiptItem>()
            .Property(item => item.UnitPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ReceiptItem>()
            .Property(item => item.TotalPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ReceiptItem>()
            .Property(item => item.Confidence)
            .HasPrecision(5, 4);

        modelBuilder.Entity<ReceiptItemAdjustment>()
            .Property(adjustment => adjustment.Amount)
            .HasPrecision(10, 2);

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

        modelBuilder.Entity<Receipt>()
            .HasIndex(receipt => receipt.PurchaseDate);
    }
}
