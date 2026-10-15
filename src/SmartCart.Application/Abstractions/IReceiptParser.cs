using SmartCart.Application.Receipts;

namespace SmartCart.Application.Abstractions;

public interface IReceiptParser
{
    bool CanParse(string retailer);

    ParsedReceipt Parse(string rawText);
}
