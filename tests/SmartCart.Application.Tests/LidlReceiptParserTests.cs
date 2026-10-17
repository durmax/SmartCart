using SmartCart.Application.Abstractions;
using SmartCart.Application.Services;
using SmartCart.Domain.Entities;
using SmartCart.Infrastructure.Receipts.Lidl;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartCart.Application.Tests;

public sealed class LidlReceiptParserTests
{
    private const string RawReceipt = """
LIDL

Am Hummelbach 40
41469 Neuss

EUR
Banane lose                        1,43 A
1,106 kg x 1,29   EUR/kg
Birnen                             2,29 A
Mango                              1,11 A
Mandarinen                         1,99 A
Zitronen                           0,99 A
Traub dunk 1kg                     3,99 A
XXL Hä.-Schn.gegart                3,99 A
Kartoffel-Gitter                   1,89 A
Easy Snack Mozzarell               4,99 A
Käsetaschen                        3,79 A
Hähnchenbr. Jalapeno               1,49 A
Preisvorteil                 -0,10
Lidl Plus Rabatt             -0,20
Mozzarella Classic                 1,29 A
Gouda Scheiben                     2,49 A
Preisvorteil                 -0,04
Gurkensalat Joghurt                1,49 A
Big BurritosTexMex                 2,99 A
Hela Ketchup delikat               3,39 A
SalamiSsticks Classic 1,89 x   2    3,78 A
Roggen Knäckebrot                  1,29 A
Butter Croissan                    0,49 A
Geflügel HotDog       1,29 x   3    3,87 A
veganes Knusperbrot                1,19 A
Cruspies Paprika      1,19 x   2    2,38 A
Preisvorteil                 -0,40
Sushi Tokyo Family                 6,39 A
Socke M 40-4-0550382               7,99 B
Preisvorteil                 -1,00
BIC Schreibw-0484155 1,99 x   2    3,98 B
Preisvorteil                 -0,40
zu zahlen                         68,82
Karte                             68,82

13.10.2026      14:23      T-ID 60152526
TA-Nr. 247453             Beleg-Nr. 8466
""";

    [Fact]
    public void CanParse_IsCaseInsensitiveAndTrimsRetailer()
    {
        Assert.True(new LidlReceiptParser().CanParse(" lidl "));
        Assert.False(new LidlReceiptParser().CanParse("Aldi"));
    }

    [Fact]
    public void Parse_RealOcrSample_ReturnsMetadataAndStructuredItems()
    {
        var result = new LidlReceiptParser().Parse(RawReceipt);

        Assert.Equal(68.82m, result.TotalAmount);
        Assert.Equal("8466", result.ReceiptNumber);
        Assert.Equal(new DateTimeOffset(2026, 10, 13, 14, 23, 0, TimeSpan.Zero), result.PurchaseDate);
        Assert.Equal(25, result.Items.Count);

        var banana = Assert.Single(result.Items.Where(item => item.ParsedName == "Banane lose"));
        Assert.Equal(1.106m, banana.Quantity);
        Assert.Equal("kg", banana.Unit);
        Assert.Equal(1.29m, banana.UnitPrice);
        Assert.Equal(1.43m, banana.TotalPrice);
        Assert.Contains("1,106 kg x 1,29   EUR/kg", banana.OriginalText);

        var salami = Assert.Single(result.Items.Where(item => item.ParsedName == "SalamiSsticks Classic"));
        Assert.Equal(2m, salami.Quantity);
        Assert.Equal("piece", salami.Unit);
        Assert.Equal(1.89m, salami.UnitPrice);
        Assert.Equal(3.78m, salami.TotalPrice);

        var chicken = Assert.Single(result.Items.Where(item => item.ParsedName == "Hähnchenbr. Jalapeno"));
        Assert.Equal(1.49m, chicken.TotalPrice);
        Assert.Collection(
            chicken.Adjustments,
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

        Assert.DoesNotContain(result.Items, item =>
            item.ParsedName is not null &&
            new[] { "MWST", "TSE", "Kartenzahlung", "Lidl Plus Angebote" }
                .Contains(item.ParsedName, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ParseAsync_PersistsMetadataItemsAndAdjustmentsWithoutProductMatching()
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            Retailer = "Lidl",
            RawOcrText = RawReceipt
        };
        var persistence = new FakeReceiptPersistence(receipt);
        var service = new ReceiptService(
            new FakeImageStorage(),
            persistence,
            new FakeOcrService(),
            [new LidlReceiptParser()],
            NullLogger<ReceiptService>.Instance);

        var result = await service.ParseAsync(receipt.Id);

        Assert.NotNull(result);
        Assert.Equal("8466", receipt.ReceiptNumber);
        Assert.Equal(68.82m, receipt.TotalAmount);
        Assert.Equal(25, receipt.Items.Count);
        Assert.All(receipt.Items, item => Assert.Null(item.ProductId));
        Assert.Equal(6, receipt.Items.Sum(item => item.Adjustments.Count));
        Assert.Equal(1.43m, receipt.Items.Single(item => item.ParsedName == "Banane lose").TotalPrice);
    }

    private sealed class FakeImageStorage : IReceiptImageStorage
    {
        public Task<string> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken = default) =>
            Task.FromResult("unused");

        public Task<Stream> OpenReadAsync(string imagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());
    }

    private sealed class FakeOcrService : IOcrService
    {
        public Task<string> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);
    }

    private sealed class FakeReceiptPersistence(Receipt receipt) : IReceiptPersistence
    {
        public Task AddAsync(Receipt newReceipt, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Receipt?>(receipt.Id == id ? receipt : null);

        public Task<Receipt?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Receipt?>(receipt.Id == id ? receipt : null);

        public Task SaveRawOcrTextAsync(Guid id, string rawOcrText, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveParsedReceiptAsync(Receipt parsedReceipt, bool force, CancellationToken cancellationToken = default)
        {
            receipt.ReceiptNumber = parsedReceipt.ReceiptNumber;
            receipt.PurchaseDate = parsedReceipt.PurchaseDate;
            receipt.TotalAmount = parsedReceipt.TotalAmount;
            receipt.Items = parsedReceipt.Items;
            return Task.CompletedTask;
        }
    }
}
