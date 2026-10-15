using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartCart.Api.Models;

public sealed class CreateReceiptRequest
{
    [Required]
    public string Retailer { get; init; } = string.Empty;

    [Required]
    public IFormFile? File { get; init; }
}
