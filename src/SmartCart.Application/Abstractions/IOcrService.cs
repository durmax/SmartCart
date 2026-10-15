namespace SmartCart.Application.Abstractions;

public interface IOcrService
{
    Task<string> ExtractTextAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
}
