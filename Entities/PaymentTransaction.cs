using Payment_Integration_API.Models;

namespace Payment_Integration_API.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public PaymentProvider Provider { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? RawRequestJson { get; set; }
    public string? RawResponseJson { get; set; }
}