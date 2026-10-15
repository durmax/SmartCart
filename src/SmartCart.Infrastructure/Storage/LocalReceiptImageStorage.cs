using SmartCart.Application.Abstractions;

namespace SmartCart.Infrastructure.Storage;

public sealed class LocalReceiptImageStorage(string storagePath, string contentRootPath) : IReceiptImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Only .jpg, .jpeg, and .png images are supported.", nameof(fileName));
        }

        var fileId = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var configuredRoot = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.Combine(contentRootPath, storagePath);
        var absoluteRoot = Path.GetFullPath(configuredRoot);
        Directory.CreateDirectory(absoluteRoot);

        var filePath = Path.Combine(absoluteRoot, fileId);
        await using (var destination = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous))
        {
            await stream.CopyToAsync(destination, cancellationToken);
        }

        return Path.Combine(storagePath, fileId).Replace(Path.DirectorySeparatorChar, '/');
    }
}
