using Microsoft.EntityFrameworkCore;
using Payment_Integration_API.Data;
using Payment_Integration_API.Entities;
using Payment_Integration_API.Models;

namespace Payment_Integration_API.Services;

public class PaymentService
{
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly PaymentDbContext _dbContext;

    public PaymentService(IPaymentProviderFactory providerFactory, PaymentDbContext dbContext)
    {
        _providerFactory = providerFactory;
        _dbContext = dbContext;
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
}