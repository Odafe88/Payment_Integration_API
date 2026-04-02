# Payment Integration API

ASP.NET Core .NET 10 backend for Flutterwave, Paystack, and Interswitch bill payment integration.

## Setup

1. Update the secrets in `appsettings.Development.json` or use user secrets:
   - `PaymentProviders:Flutterwave:SecretKey` and `WebhookSecret` (verif-hash from dashboard)
   - `PaymentProviders:Paystack:SecretKey` and `WebhookSecret` (same as SecretKey)
   - `PaymentProviders:Interswitch:ClientId/ClientSecret` and `WebhookSecret`

2. Configure DB (SQLite by default) in `ConnectionStrings:DefaultConnection`.

3. Apply EF migrations:
   - `dotnet ef database update`

4. Run:
   - `dotnet run`

## Endpoints

- POST `/api/payments/initiate` (body: `PaymentRequest`)
- GET `/api/payments/status/{reference}?refresh=false` (polling alternative)
- POST `/api/payments/verify` (manual verification)
- POST `/api/payments/webhook/paystack` (webhook for Paystack)
- POST `/api/payments/webhook/flutterwave` (webhook for Flutterwave)
- POST `/api/payments/webhook/interswitch` (webhook for Interswitch)

## Webhook Setup

Configure webhook URLs in provider dashboards:
- Paystack: `https://yourdomain.com/api/payments/webhook/paystack`
- Flutterwave: `https://yourdomain.com/api/payments/webhook/flutterwave`
- Interswitch: `https://yourdomain.com/api/payments/webhook/interswitch`

Webhooks verify signatures and update transaction status automatically.

## Notes

- Interswitch provider is scaffolded; implement Quickteller API for full support.
- For production, use HTTPS and secure secrets.
- Add webhook verification for production: `x-paystack-signature`, `verif-hash`, HMAC for Interswitch.
