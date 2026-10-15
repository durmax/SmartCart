namespace SmartCart.Application.Abstractions;

public interface IReceiptImageStorage
{
    Task<string> SaveAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default);
}
