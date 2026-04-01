using Payment_Integration_API.Models;

namespace Payment_Integration_API.Services;

public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(PaymentProvider provider);
}

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPaymentProvider GetProvider(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.Flutterwave => _serviceProvider.GetRequiredService<FlutterwaveProvider>(),
            PaymentProvider.Paystack => _serviceProvider.GetRequiredService<PaystackProvider>(),
            PaymentProvider.Interswitch => _serviceProvider.GetRequiredService<InterswitchProvider>(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }
}
