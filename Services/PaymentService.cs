using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Payment_Integration_API.Data;
using Payment_Integration_API.Entities;
using Payment_Integration_API.Models;
using Payment_Integration_API.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

namespace Payment_Integration_API.Services;

public class PaymentService
{
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly PaymentDbContext _dbContext;
    private readonly FlutterwaveOptions _flutterwaveOptions;
    private readonly PaystackOptions _paystackOptions;
    private readonly InterswitchOptions _interswitchOptions;

    public PaymentService(
        IPaymentProviderFactory providerFactory,
        PaymentDbContext dbContext,
        IOptions<FlutterwaveOptions> flutterwaveOptions,
        IOptions<PaystackOptions> paystackOptions,
        IOptions<InterswitchOptions> interswitchOptions)
    {
        _providerFactory = providerFactory;
        _dbContext = dbContext;
        _flutterwaveOptions = flutterwaveOptions.Value;
        _paystackOptions = paystackOptions.Value;
        _interswitchOptions = interswitchOptions.Value;
    }

    public async Task<PaymentResult> InitiateAsync(PaymentRequest request)
    {
        var provider = _providerFactory.GetProvider(request.Provider);
        var result = await provider.ChargeCustomerAsync(request);

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Provider = request.Provider,
            Reference = request.Reference,
            TransactionId = result.TransactionId,
            Amount = request.Amount,
            Currency = request.Currency,
            CustomerEmail = request.CustomerEmail,
            Status = result.Status,
            IsSuccess = result.Success,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RawRequestJson = System.Text.Json.JsonSerializer.Serialize(request),
            RawResponseJson = result.RawResponseJson
        };

        _dbContext.PaymentTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        return result;
    }

    public async Task<PaymentVerificationResult> VerifyAsync(PaymentProvider provider, string reference)
    {
        var paymentProvider = _providerFactory.GetProvider(provider);
        var result = await paymentProvider.VerifyAsync(reference);

        if (!string.IsNullOrWhiteSpace(reference))
        {
            var existing = await _dbContext.PaymentTransactions.FirstOrDefaultAsync(x => x.Reference == reference);
            if (existing != null)
            {
                existing.Status = result.Status;
                existing.IsSuccess = result.Success;
                existing.TransactionId = result.TransactionId ?? existing.TransactionId;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.RawResponseJson = result.RawResponseJson;
                await _dbContext.SaveChangesAsync();
            }
        }

        return result;
    }

    public async Task<PaymentStatusResult> GetStatusAsync(string reference, bool refresh = false)
    {
        var transaction = await _dbContext.PaymentTransactions.FirstOrDefaultAsync(x => x.Reference == reference);
        if (transaction == null)
            return new PaymentStatusResult { Reference = reference, Status = "not_found" };

        // If refresh requested or status is still pending/initialized, verify with provider
        if (refresh || IsPendingStatus(transaction.Status))
        {
            var verification = await VerifyAsync(transaction.Provider, reference);
            // Update transaction if verification succeeded
            if (verification.Success || !string.IsNullOrEmpty(verification.Status))
            {
                transaction.Status = verification.Status;
                transaction.IsSuccess = verification.Success;
                transaction.TransactionId = verification.TransactionId ?? transaction.TransactionId;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        return new PaymentStatusResult
        {
            Reference = transaction.Reference,
            Provider = transaction.Provider,
            Status = transaction.Status,
            IsSuccess = transaction.IsSuccess,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            TransactionId = transaction.TransactionId
        };
    }

    private bool IsPendingStatus(string status)
    {
        return status.Equals("initialized", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("pending", StringComparison.OrdinalIgnoreCase) ||
               string.IsNullOrEmpty(status);
    }

    public async Task<bool> ProcessWebhookAsync(PaymentProvider provider, HttpRequest request)
    {
        try
        {
            // Read raw body
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Verify signature
            if (!VerifyWebhookSignature(provider, body, request.Headers))
                return false;

            // Parse payload
            var payload = JsonDocument.Parse(body).RootElement;

            string reference;
            string status;
            string? transactionId = null;

            switch (provider)
            {
                case PaymentProvider.Paystack:
                    reference = payload.GetProperty("data").GetProperty("reference").GetString() ?? "";
                    var eventType = payload.GetProperty("event").GetString();
                    status = eventType == "charge.success" ? "success" : "failed";
                    transactionId = payload.GetProperty("data").GetProperty("id").GetInt32().ToString();
                    break;
                case PaymentProvider.Flutterwave:
                    reference = payload.GetProperty("data").GetProperty("tx_ref").GetString() ?? "";
                    status = payload.GetProperty("data").GetProperty("status").GetString() ?? "";
                    transactionId = payload.GetProperty("data").GetProperty("id").GetInt32().ToString();
                    break;
                case PaymentProvider.Interswitch:
                    // Placeholder: adjust based on Interswitch webhook payload
                    reference = payload.GetProperty("reference").GetString() ?? "";
                    status = payload.GetProperty("status").GetString() ?? "";
                    transactionId = payload.GetProperty("transactionId").GetString();
                    break;
                default:
                    return false;
            }

            // Update DB
            var transaction = await _dbContext.PaymentTransactions.FirstOrDefaultAsync(x => x.Reference == reference);
            if (transaction != null)
            {
                transaction.Status = status;
                transaction.IsSuccess = status.Equals("success", StringComparison.OrdinalIgnoreCase);
                transaction.TransactionId = transactionId ?? transaction.TransactionId;
                transaction.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool VerifyWebhookSignature(PaymentProvider provider, string body, IHeaderDictionary headers)
    {
        switch (provider)
        {
            case PaymentProvider.Paystack:
            {
                var paystackSignature = headers["x-paystack-signature"].ToString();
                if (string.IsNullOrEmpty(paystackSignature)) return false;
                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_paystackOptions.WebhookSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
                return paystackSignature == expectedSignature;
            }

            case PaymentProvider.Flutterwave:
                var flutterwaveHash = headers["verif-hash"].ToString();
                return flutterwaveHash == _flutterwaveOptions.WebhookSecret;

            case PaymentProvider.Interswitch:
            {
                // Placeholder: implement HMAC with client secret
                var interswitchSignature = headers["signature"].ToString();
                if (string.IsNullOrEmpty(interswitchSignature)) return false;
                using var hmacInt = new HMACSHA256(Encoding.UTF8.GetBytes(_interswitchOptions.WebhookSecret));
                var hashInt = hmacInt.ComputeHash(Encoding.UTF8.GetBytes(body));
                var expectedInt = BitConverter.ToString(hashInt).Replace("-", "").ToLower();
                return interswitchSignature == expectedInt;
            }

            default:
                return false;
        }
    }
}