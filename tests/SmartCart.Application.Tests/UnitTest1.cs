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

    [Fact]
    public async Task GetDetailsByIdAsync_ReceiptWithItemsAndAdjustments_ReturnsPersistedValues()
    {
        var receipt = CreateReceipt();
        receipt.ReceiptNumber = "8466";
        receipt.PurchaseDate = new DateTimeOffset(2026, 10, 13, 14, 23, 0, TimeSpan.Zero);
        receipt.TotalAmount = 68.82m;
        receipt.StoreNumber = "123";
        receipt.RawOcrText = "persisted OCR text";

        var firstItem = new ReceiptItem
        {
            Id = Guid.NewGuid(),
            ReceiptId = receipt.Id,
            Receipt = receipt,
            OriginalText = "Banane lose 1,43 A",
            ParsedName = "Banane lose",
            Quantity = 1.106m,
            Unit = "kg",
            UnitPrice = 1.29m,
            TotalPrice = 1.43m,
            ProductId = Guid.NewGuid(),
            Confidence = 0.9876m
        };
        firstItem.Adjustments.Add(new ReceiptItemAdjustment
        {
            Id = Guid.NewGuid(),
            ReceiptItemId = firstItem.Id,
            ReceiptItem = firstItem,
            Description = "Preisvorteil",
            Amount = -0.10m
        });
        firstItem.Adjustments.Add(new ReceiptItemAdjustment
        {
            Id = Guid.NewGuid(),
            ReceiptItemId = firstItem.Id,
            ReceiptItem = firstItem,
            Description = "Lidl Plus Rabatt",
            Amount = -0.20m
        });

        var secondItem = new ReceiptItem
        {
            Id = Guid.NewGuid(),
            ReceiptId = receipt.Id,
            Receipt = receipt,
            OriginalText = "Birnen 2,29 A",
            ParsedName = "Birnen",
            Quantity = 2m,
            Unit = "piece",
            UnitPrice = 1.145m,
            TotalPrice = 2.29m
        };
        secondItem.Adjustments.Add(new ReceiptItemAdjustment
        {
            Id = Guid.NewGuid(),
            ReceiptItemId = secondItem.Id,
            ReceiptItem = secondItem,
            Description = "Sonderangebot",
            Amount = -0.15m
        });
        receipt.Items = [firstItem, secondItem];

        var ocrService = new FakeOcrService("must not run");
        var persistence = new FakeReceiptPersistence(receipt);
        var service = CreateService(
            new FakeReceiptImageStorage("receipts/receipt.png"),
            persistence,
            ocrService);

        var result = await service.GetDetailsByIdAsync(receipt.Id);

        Assert.NotNull(result);
        Assert.Equal(receipt.Id, result.Id);
        Assert.Equal("Lidl", result.Retailer);
        Assert.Equal("8466", result.ReceiptNumber);
        Assert.Equal(receipt.PurchaseDate, result.PurchaseDate);
        Assert.Equal(68.82m, result.TotalAmount);
        Assert.Equal("123", result.StoreNumber);
        Assert.Equal(receipt.ImagePath, result.ImagePath);
        Assert.Equal(receipt.CreatedAt, result.CreatedAt);
        Assert.Equal("persisted OCR text", result.RawOcrText);
        Assert.Equal(2, result.Items.Count);

        var mappedFirstItem = result.Items[0];
        Assert.Equal(firstItem.Id, mappedFirstItem.Id);
        Assert.Equal("Banane lose 1,43 A", mappedFirstItem.OriginalText);
        Assert.Equal("Banane lose", mappedFirstItem.ParsedName);
        Assert.Equal(1.106m, mappedFirstItem.Quantity);
        Assert.Equal("kg", mappedFirstItem.Unit);
        Assert.Equal(1.29m, mappedFirstItem.UnitPrice);
        Assert.Equal(1.43m, mappedFirstItem.TotalPrice);
        Assert.Equal(firstItem.ProductId, mappedFirstItem.ProductId);
        Assert.Equal(0.9876m, mappedFirstItem.Confidence);
        Assert.Collection(
            mappedFirstItem.Adjustments,
            adjustment =>
            {
                Assert.Equal("Preisvorteil", adjustment.Description);
                Assert.Equal(-0.10m, adjustment.Amount);
            },
            adjustment =>
            {
                Assert.Equal("Lidl Plus Rabatt", adjustment.Description);
                Assert.Equal(-0.20m, adjustment.Amount);
            });

        Assert.Equal("Birnen", result.Items[1].ParsedName);
        Assert.Single(result.Items[1].Adjustments);
        Assert.Equal(0, ocrService.CallCount);
    }

    [Fact]
    public async Task GetDetailsByIdAsync_ReceiptWithoutParsedItems_ReturnsEmptyItems()
    {
        var receipt = CreateReceipt();
        var ocrService = new FakeOcrService("must not run");
        var service = CreateService(
            new FakeReceiptImageStorage("receipts/receipt.png"),
            new FakeReceiptPersistence(receipt),
            ocrService);

        var result = await service.GetDetailsByIdAsync(receipt.Id);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, ocrService.CallCount);
    }

    [Fact]
    public async Task GetDetailsByIdAsync_MissingReceipt_ReturnsNull()
    {
        var service = CreateService(
            new FakeReceiptImageStorage("receipts/receipt.png"),
            new FakeReceiptPersistence(),
            new FakeOcrService("must not run"));

        var result = await service.GetDetailsByIdAsync(Guid.NewGuid());

        Assert.Null(result);
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

        public Task<Receipt?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
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
