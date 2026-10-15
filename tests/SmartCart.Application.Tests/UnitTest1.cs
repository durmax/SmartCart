using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartCart.Application.Abstractions;
using SmartCart.Application.Services;
using SmartCart.Domain.Entities;
using SmartCart.Infrastructure.Receipts.Lidl;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartCart.Application.Tests;

public class ReceiptServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidUpload_PersistsReceiptAndReturnsReceiptInformation()
    {
        var imageStorage = new FakeReceiptImageStorage("receipts/generated.jpg");
        var persistence = new FakeReceiptPersistence();
        var ocrService = new FakeOcrService("ignored");
        var service = CreateService(imageStorage, persistence, ocrService);
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

    [Fact]
    public async Task ProcessOcrAsync_ExistingReceipt_StoresAndReturnsOcrText()
    {
        var receipt = CreateReceipt();
        var persistence = new FakeReceiptPersistence(receipt);
        var imageStorage = new FakeReceiptImageStorage("receipts/receipt.png");
        var ocrService = new FakeOcrService("LIDL\n68,82");
        var service = CreateService(imageStorage, persistence, ocrService);

        var result = await service.ProcessOcrAsync(receipt.Id);

        Assert.Equal("LIDL\n68,82", result);
        Assert.Equal("LIDL\n68,82", receipt.RawOcrText);
        Assert.Equal(1, ocrService.CallCount);
        Assert.Equal(receipt.ImagePath, imageStorage.OpenedImagePath);
        Assert.Equal(1, persistence.SaveOcrTextCallCount);
        Assert.Empty(receipt.Items);
        Assert.Null(receipt.ReceiptNumber);
        Assert.Null(receipt.PurchaseDate);
        Assert.Null(receipt.TotalAmount);
        Assert.Null(receipt.StoreNumber);
    }

    [Fact]
    public async Task ProcessOcrAsync_ExistingTextWithoutForce_ReturnsTextWithoutOcr()
    {
        var receipt = CreateReceipt();
        receipt.RawOcrText = "cached";
        var persistence = new FakeReceiptPersistence(receipt);
        var ocrService = new FakeOcrService("new");
        var service = CreateService(new FakeReceiptImageStorage("receipts/receipt.png"), persistence, ocrService);

        var result = await service.ProcessOcrAsync(receipt.Id);

        Assert.Equal("cached", result);
        Assert.Equal(0, ocrService.CallCount);
        Assert.Equal(0, persistence.SaveOcrTextCallCount);
    }

    [Fact]
    public async Task ProcessOcrAsync_Force_RunsOcrAndReplacesExistingText()
    {
        var receipt = CreateReceipt();
        receipt.RawOcrText = "cached";
        var persistence = new FakeReceiptPersistence(receipt);
        var ocrService = new FakeOcrService("refreshed");
        var service = CreateService(new FakeReceiptImageStorage("receipts/receipt.png"), persistence, ocrService);

        var result = await service.ProcessOcrAsync(receipt.Id, force: true);

        Assert.Equal("refreshed", result);
        Assert.Equal("refreshed", receipt.RawOcrText);
        Assert.Equal(1, ocrService.CallCount);
    }

    [Fact]
    public async Task ProcessOcrAsync_MissingReceipt_ReturnsNullWithoutRunningOcr()
    {
        var persistence = new FakeReceiptPersistence();
        var ocrService = new FakeOcrService("unused");
        var service = CreateService(new FakeReceiptImageStorage("receipts/receipt.png"), persistence, ocrService);

        var result = await service.ProcessOcrAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Equal(0, ocrService.CallCount);
    }

    private static Receipt CreateReceipt() => new()
    {
        Id = Guid.NewGuid(),
        Retailer = "Lidl",
        ImagePath = "receipts/receipt.png",
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static ReceiptService CreateService(
        FakeReceiptImageStorage imageStorage,
        FakeReceiptPersistence persistence,
        FakeOcrService ocrService) => new(
            imageStorage,
            persistence,
            ocrService,
            [],
            NullLogger<ReceiptService>.Instance);

    private sealed class FakeReceiptImageStorage : IReceiptImageStorage
    {
        private readonly string _imagePath;
        public string? FileName { get; private set; }
        public string? OpenedImagePath { get; private set; }

        public FakeReceiptImageStorage(string imagePath) => _imagePath = imagePath;

        public Task<string> SaveAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            return Task.FromResult(_imagePath);
        }

        public Task<Stream> OpenReadAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            OpenedImagePath = imagePath;
            return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));
        }
    }

    private sealed class FakeReceiptPersistence : IReceiptPersistence
    {
        private readonly Receipt? _initialReceipt;

        public Receipt? AddedReceipt { get; private set; }
        public int SaveOcrTextCallCount { get; private set; }

        public FakeReceiptPersistence(Receipt? initialReceipt = null) => _initialReceipt = initialReceipt;

        public Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
        {
            AddedReceipt = receipt;
            return Task.CompletedTask;
        }

        public Task SaveParsedReceiptAsync(Receipt receipt, bool force, CancellationToken cancellationToken = default)
        {
            var existingReceipt = AddedReceipt ?? _initialReceipt;
            if (existingReceipt is not null && (force || existingReceipt.Items.Count == 0))
            {
                existingReceipt.ReceiptNumber = receipt.ReceiptNumber;
                existingReceipt.PurchaseDate = receipt.PurchaseDate;
                existingReceipt.TotalAmount = receipt.TotalAmount;
                existingReceipt.Items = receipt.Items;
            }

            return Task.CompletedTask;
        }

        public Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var receipt = AddedReceipt ?? _initialReceipt;
            return Task.FromResult(receipt?.Id == id ? receipt : null);
        }

        public Task SaveRawOcrTextAsync(Guid id, string rawOcrText, CancellationToken cancellationToken = default)
        {
            var receipt = AddedReceipt ?? _initialReceipt;
            if (receipt?.Id == id)
            {
                receipt.RawOcrText = rawOcrText;
                SaveOcrTextCallCount++;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeOcrService(string result) : IOcrService
    {
        public int CallCount { get; private set; }

        public Task<string> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }
}
