using System.Globalization;
using System.Text.RegularExpressions;
using SmartCart.Application.Abstractions;

namespace SmartCart.Application.Receipts.Lidl;

public sealed class LidlReceiptParser : IReceiptParser
{
    private static readonly Regex AmountPattern = new(@"-?\d+[,.]\d{2}", RegexOptions.Compiled);
    private static readonly Regex MultiQuantityPattern = new(
        @"^(?<name>.+?)\s+(?<unitPrice>\d+[,.]\d{2})\s*x\s*(?<quantity>\d+)\s+(?<totalPrice>\d+[,.]\d{2})\s+[AB]\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex WeightedPattern = new(
        @"^(?<quantity>\d+[,.]\d+)\s*kg\s*x\s*(?<unitPrice>\d+[,.]\d{2})\s*(?:EUR\s*/\s*kg|€/kg)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProductPattern = new(
        @"^(?<name>.+?)\s+(?<totalPrice>\d+[,.]\d{2})\s+[AB]\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AdjustmentPattern = new(
        @"^(?<description>.+?)\s+(?<amount>-\d+[,.]\d{2})\s*$",
        RegexOptions.Compiled);
    private static readonly Regex ReceiptNumberPattern = new(
        @"Beleg-Nr\.\s*(?<number>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PurchaseDatePattern = new(
        @"(?<date>\d{1,2}\.\d{1,2}\.\d{4})\s+(?<time>\d{1,2}:\d{2})", RegexOptions.Compiled);

    public bool CanParse(string retailer) =>
        string.Equals(retailer?.Trim(), "Lidl", StringComparison.OrdinalIgnoreCase);

    public ParsedReceipt Parse(string rawText)
    {
        ArgumentNullException.ThrowIfNull(rawText);

        var lines = rawText.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        var receiptNumber = ParseReceiptNumber(rawText);
        var purchaseDate = ParsePurchaseDate(rawText);
        var totalAmount = ParseTotalAmount(lines);
        var items = new List<MutableItem>();
        var inProducts = false;

        foreach (var originalLine in lines)
        {
            var line = originalLine.Trim();
            if (!inProducts)
            {
                if (string.Equals(line, "EUR", StringComparison.OrdinalIgnoreCase))
                {
                    inProducts = true;
                }

                continue;
            }

            if (line.StartsWith("zu zahlen", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (line.Length == 0)
            {
                continue;
            }

            var weightedMatch = WeightedPattern.Match(line);
            if (weightedMatch.Success && items.Count > 0)
            {
                var previousItem = items[^1];
                previousItem.Quantity = ParseGermanDecimal(weightedMatch.Groups["quantity"].Value);
                previousItem.Unit = "kg";
                previousItem.UnitPrice = ParseGermanDecimal(weightedMatch.Groups["unitPrice"].Value);
                previousItem.OriginalText = $"{previousItem.OriginalText}{Environment.NewLine}{originalLine.TrimEnd()}";
                continue;
            }

            var adjustmentMatch = AdjustmentPattern.Match(line);
            if (adjustmentMatch.Success && items.Count > 0)
            {
                items[^1].Adjustments.Add(new ParsedReceiptItemAdjustment
                {
                    Description = adjustmentMatch.Groups["description"].Value.Trim(),
                    Amount = ParseGermanDecimal(adjustmentMatch.Groups["amount"].Value)
                });
                continue;
            }

            var multiQuantityMatch = MultiQuantityPattern.Match(line);
            if (multiQuantityMatch.Success)
            {
                var unitPrice = ParseGermanDecimal(multiQuantityMatch.Groups["unitPrice"].Value);
                items.Add(new MutableItem
                {
                    OriginalText = originalLine.TrimEnd(),
                    ParsedName = multiQuantityMatch.Groups["name"].Value.Trim(),
                    Quantity = ParseGermanDecimal(multiQuantityMatch.Groups["quantity"].Value),
                    Unit = "piece",
                    UnitPrice = unitPrice,
                    TotalPrice = ParseGermanDecimal(multiQuantityMatch.Groups["totalPrice"].Value)
                });
                continue;
            }

            var productMatch = ProductPattern.Match(line);
            if (productMatch.Success)
            {
                var totalPrice = ParseGermanDecimal(productMatch.Groups["totalPrice"].Value);
                items.Add(new MutableItem
                {
                    OriginalText = originalLine.TrimEnd(),
                    ParsedName = productMatch.Groups["name"].Value.Trim(),
                    Quantity = 1m,
                    UnitPrice = totalPrice,
                    TotalPrice = totalPrice
                });
            }
        }

        return new ParsedReceipt
        {
            ReceiptNumber = receiptNumber,
            PurchaseDate = purchaseDate,
            TotalAmount = totalAmount,
            Items = items.Select(item => new ParsedReceiptItem
            {
                OriginalText = item.OriginalText,
                ParsedName = item.ParsedName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                Adjustments = item.Adjustments
            }).ToList()
        };
    }

    private static string? ParseReceiptNumber(string rawText)
    {
        var match = ReceiptNumberPattern.Match(rawText);
        return match.Success ? match.Groups["number"].Value : null;
    }

    private static DateTimeOffset? ParsePurchaseDate(string rawText)
    {
        var match = PurchaseDatePattern.Match(rawText);
        if (!match.Success ||
            !DateTime.TryParseExact(
                $"{match.Groups["date"].Value} {match.Groups["time"].Value}",
                "d.M.yyyy H:mm",
                CultureInfo.GetCultureInfo("de-DE"),
                DateTimeStyles.None,
                out var purchaseDate))
        {
            return null;
        }

        return new DateTimeOffset(purchaseDate, TimeSpan.Zero);
    }

    private static decimal? ParseTotalAmount(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("zu zahlen", StringComparison.OrdinalIgnoreCase))
            {
                var match = AmountPattern.Match(line);
                if (match.Success)
                {
                    return ParseGermanDecimal(match.Value);
                }
            }
        }

        return null;
    }

    private static decimal ParseGermanDecimal(string value) =>
        decimal.Parse(value.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    private sealed class MutableItem
    {
        public string OriginalText { get; set; } = string.Empty;
        public string? ParsedName { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public List<ParsedReceiptItemAdjustment> Adjustments { get; } = [];
    }
}
