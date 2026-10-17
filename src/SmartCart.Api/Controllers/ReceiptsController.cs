using Microsoft.AspNetCore.Mvc;
using SmartCart.Api.Models;
using SmartCart.Application.Receipts;
using SmartCart.Application.Services;

namespace SmartCart.Api.Controllers;

[ApiController]
[Route("api/receipts")]
public sealed class ReceiptsController(IReceiptService receiptService) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType<CreateReceiptResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateReceiptResult>> CreateAsync(
        [FromForm] CreateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Retailer))
        {
            ModelState.AddModelError(nameof(request.Retailer), "Retailer is required.");
        }

        if (request.File is null)
        {
            ModelState.AddModelError(nameof(request.File), "A receipt image is required.");
        }
        else
        {
            var extension = Path.GetExtension(request.File.FileName);
            if (request.File.Length == 0)
            {
                ModelState.AddModelError(nameof(request.File), "The uploaded file is empty.");
            }
            else if (request.File.Length > MaxFileSize)
            {
                ModelState.AddModelError(nameof(request.File), "The maximum upload size is 10 MB.");
            }
            else if (!AllowedContentTypes.TryGetValue(extension, out var expectedContentType))
            {
                ModelState.AddModelError(nameof(request.File), "Only .jpg, .jpeg, and .png images are supported.");
            }
            else if (!string.Equals(request.File.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(request.File), "The image MIME type does not match its file extension.");
            }
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await using var imageStream = request.File!.OpenReadStream();
        var result = await receiptService.CreateAsync(
            request.Retailer,
            imageStream,
            request.File.FileName,
            cancellationToken);

        return CreatedAtRoute(nameof(GetByIdAsync), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetByIdAsync))]
    [ProducesResponseType<CreateReceiptResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateReceiptResult>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await receiptService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType<ReceiptDetailsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceiptDetailsResult>> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await receiptService.GetDetailsByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/ocr")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessOcrAsync(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var rawOcrText = await receiptService.ProcessOcrAsync(id, force, cancellationToken);
        return rawOcrText is null
            ? NotFound()
            : Ok(new { ReceiptId = id, RawOcrText = rawOcrText });
    }

    [HttpPost("{id:guid}/parse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ParseAsync(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var result = await receiptService.ParseAsync(id, force, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
