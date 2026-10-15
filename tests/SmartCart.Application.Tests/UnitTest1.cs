using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartCart.Application.Abstractions;
using SmartCart.Application.Services;
using SmartCart.Domain.Entities;
using Xunit;

namespace SmartCart.Application.Tests;

public class ReceiptServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidUpload_PersistsReceiptAndReturnsReceiptInformation()
    {
        var imageStorage = new FakeReceiptImageStorage("receipts/generated.jpg");
        var persistence = new FakeReceiptPersistence();
        var service = new ReceiptService(imageStorage, persistence);
        await using var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await service.CreateAsync("Lidl", imageStream, "original.jpg");

        var receipt = Assert.IsType<Receipt>(persistence.AddedReceipt);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(result.Id, receipt.Id);
        Assert.Equal("Lidl", result.Retailer);
        Assert.Equal("Lidl", receipt.Retailer);
        Assert.Equal("receipts/generated.jpg", result.ImagePath);
        Assert.Equal(result.ImagePath, receipt.ImagePath);
        Assert.NotEqual(default, result.CreatedAt);
        Assert.Null(receipt.ReceiptNumber);
        Assert.Null(receipt.PurchaseDate);
        Assert.Null(receipt.TotalAmount);
        Assert.Null(receipt.StoreNumber);
        Assert.Null(receipt.RawOcrText);
        Assert.Empty(receipt.Items);
        Assert.Equal("original.jpg", imageStorage.FileName);
    }

    private sealed class FakeReceiptImageStorage : IReceiptImageStorage
    {
        private readonly string _imagePath;
        public string? FileName { get; private set; }

        public FakeReceiptImageStorage(string imagePath) => _imagePath = imagePath;

        public Task<string> SaveAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            return Task.FromResult(_imagePath);
        }
    }

    private sealed class FakeReceiptPersistence : IReceiptPersistence
    {
        public Receipt? AddedReceipt { get; private set; }

        public Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
        {
            AddedReceipt = receipt;
            return Task.CompletedTask;
        }

        public Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AddedReceipt?.Id == id ? AddedReceipt : null);
        }
    }
}
