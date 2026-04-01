using System.Text.Json;
using Microsoft.Extensions.Options;
using Payment_Integration_API.Models;
using Payment_Integration_API.Options;

namespace Payment_Integration_API.Services;

public class InterswitchProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly InterswitchOptions _options;

    public InterswitchProvider(HttpClient httpClient, IOptions<InterswitchOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<PaymentResult> ChargeCustomerAsync(PaymentRequest request)
    {
        // Implement Interswitch PayDirect flow (token + initialize) as per docs.
        return Task.FromResult(new PaymentResult
        {
            Success = false,
            Provider = PaymentProvider.Interswitch,
            Status = "unsupported",
            Message = "Interswitch implementation pending.",
            RawResponseJson = "{}"
        });
    }

    public Task<PaymentVerificationResult> VerifyAsync(string reference)
    {
        return Task.FromResult(new PaymentVerificationResult
        {
            Success = false,
            Status = "unsupported",
            RawResponseJson = "{}"
        });
    }
}
