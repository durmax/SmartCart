namespace SmartCart.Infrastructure.OCR;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    public string TessDataPath { get; set; } = "OCR/tessdata";

    public string Language { get; set; } = "deu";
}
