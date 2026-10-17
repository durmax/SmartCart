using SmartCart.Infrastructure.Receipts.Lidl;

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

}
