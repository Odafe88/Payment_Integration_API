# Payment Integration API

ASP.NET Core .NET 10 backend for Flutterwave, Paystack, and Interswitch bill payment integration.

## Setup

1. Update the secrets in `appsettings.Development.json` or use user secrets:
   - `PaymentProviders:Flutterwave:SecretKey`
   - `PaymentProviders:Paystack:SecretKey`
   - `PaymentProviders:Interswitch:ClientId/ClientSecret`

2. Configure DB (LocalDB by default) in `ConnectionStrings:DefaultConnection`.

3. Apply EF migrations:
   - `dotnet tool install --global dotnet-ef` (if needed)
   - `dotnet ef migrations add InitialCreate`
   - `dotnet ef database update`

4. Run:
   - `dotnet run`

## Endpoints

- POST `/api/payments/initiate` (body: `PaymentRequest`)
- POST `/api/payments/verify?provider=Paystack&reference=...`
- POST `/api/payments/webhook` (stub)

## Notes

- Interswitch provider is scaffolded as a stub and needs implementation of PayDirect / Quickteller token flows.
- Add webhook verification for production: `x-paystack-signature`, `verif-hash`, HMAC for Interswitch.
