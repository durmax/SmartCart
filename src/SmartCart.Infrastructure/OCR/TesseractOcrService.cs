using SmartCart.Application.Abstractions;
using Tesseract;

namespace SmartCart.Infrastructure.OCR;

public sealed class TesseractOcrService : IOcrService, IDisposable
{
    private readonly TesseractEngine _engine;
    private bool _disposed;

    public TesseractOcrService(string tessDataPath, string language)
    {
        if (!Directory.Exists(tessDataPath))
        {
            throw new DirectoryNotFoundException($"Tesseract data directory was not found: {tessDataPath}");
        }

        try
        {
            _engine = new TesseractEngine(tessDataPath, language, EngineMode.LstmOnly);
            _engine.SetVariable("preserve_interword_spaces", "1");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Tesseract OCR could not be initialized using language '{language}' and data directory '{tessDataPath}'. Ensure the trained data is deployed.",
                exception);
        }
    }

    public Task<string> ExtractTextAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(imageStream);
        if (!imageStream.CanRead)
        {
            throw new ArgumentException("Image stream must be readable.", nameof(imageStream));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var memoryStream = new MemoryStream();
            imageStream.CopyTo(memoryStream);
            cancellationToken.ThrowIfCancellationRequested();
            using var pix = Pix.LoadFromMemory(memoryStream.ToArray());
            using var page = _engine.Process(pix, PageSegMode.Auto);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(page.GetText());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException("Tesseract OCR could not process the receipt image.", exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _engine.Dispose();
        _disposed = true;
    }
}
