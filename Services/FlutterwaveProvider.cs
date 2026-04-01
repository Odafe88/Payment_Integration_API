using System.Text.Json;
using Microsoft.Extensions.Options;
using Payment_Integration_API.Models;
using Payment_Integration_API.Options;

namespace Payment_Integration_API.Services;

public class FlutterwaveProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly FlutterwaveOptions _options;

    public FlutterwaveProvider(HttpClient httpClient, IOptions<FlutterwaveOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PaymentResult> ChargeCustomerAsync(PaymentRequest request)
    {
        var payload = new
        {
            tx_ref = request.Reference,
            amount = request.Amount.ToString("F2"),
            currency = request.Currency,
            redirect_url = request.RedirectUrl,
            customer = new { email = request.CustomerEmail, phone_number = request.CustomerPhone, name = request.CustomerName },
            customizations = new { title = "Bill Payment", description = "Customer to company payment" },
            meta = request.Metadata
        };

        var httpResponse = await _httpClient.PostAsJsonAsync("/payments", payload);
        var content = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
        {
            return new PaymentResult
            {
                Success = false,
                Provider = PaymentProvider.Flutterwave,
                Status = "failed",
                Message = $"Flutterwave initialize failed: {httpResponse.StatusCode}",
                RawResponseJson = content
            };
        }

        var doc = JsonDocument.Parse(content);
        var status = doc.RootElement.GetProperty("status").GetString() ?? "failed";
        var data = doc.RootElement.GetProperty("data");
        var transactionId = data.GetProperty("id").GetInt32().ToString();
        var link = data.GetProperty("link").GetString();

        return new PaymentResult
        {
            Success = status.Equals("success", StringComparison.OrdinalIgnoreCase),
            Provider = PaymentProvider.Flutterwave,
            TransactionId = transactionId,
            Status = status,
            RedirectUrl = link,
            RawResponseJson = content
        };
    }

    public async Task<PaymentVerificationResult> VerifyAsync(string reference)
    {
        var httpResponse = await _httpClient.GetAsync($"/transactions/verify_by_reference?tx_ref={Uri.EscapeDataString(reference)}");
        var content = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
        {
            return new PaymentVerificationResult { Success = false, Status = "failed", RawResponseJson = content };
        }

        var doc = JsonDocument.Parse(content);
        var status = doc.RootElement.GetProperty("status").GetString() ?? "failed";
        var data = doc.RootElement.GetProperty("data");
        var transactionId = data.GetProperty("id").GetInt32().ToString();
        return new PaymentVerificationResult { Success = status.Equals("success", StringComparison.OrdinalIgnoreCase), Status = data.GetProperty("status").GetString() ?? "unknown", TransactionId = transactionId, RawResponseJson = content };
    }
}
