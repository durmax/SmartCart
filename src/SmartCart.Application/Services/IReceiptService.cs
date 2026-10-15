namespace SmartCart.Application.Services;

public interface IReceiptService
{
    Task<CreateReceiptResult> CreateAsync(
        string retailer,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<CreateReceiptResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
