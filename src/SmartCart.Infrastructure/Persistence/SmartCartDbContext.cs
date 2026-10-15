using Microsoft.EntityFrameworkCore;
using SmartCart.Domain.Entities;

namespace SmartCart.Infrastructure.Persistence;

public class SmartCartDbContext(DbContextOptions<SmartCartDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();

    public DbSet<Offer> Offers => Set<Offer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
