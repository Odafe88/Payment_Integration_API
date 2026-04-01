using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Payment_Integration_API.Controllers;
using Payment_Integration_API.Data;
using Payment_Integration_API.Options;
using Payment_Integration_API.Services;
using Payment_Integration_API.Validators;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<PaymentRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=PaymentIntegrationDb;Integrated Security=True;"));

builder.Services.Configure<FlutterwaveOptions>(builder.Configuration.GetSection("PaymentProviders:Flutterwave"));
builder.Services.Configure<PaystackOptions>(builder.Configuration.GetSection("PaymentProviders:Paystack"));
builder.Services.Configure<InterswitchOptions>(builder.Configuration.GetSection("PaymentProviders:Interswitch"));

builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddHttpClient<FlutterwaveProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("PaymentProviders:Flutterwave:BaseUrl") ?? "https://api.flutterwave.com/v3");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration.GetValue<string>("PaymentProviders:Flutterwave:SecretKey"));
}).AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

builder.Services.AddHttpClient<PaystackProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("PaymentProviders:Paystack:BaseUrl") ?? "https://api.paystack.co");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration.GetValue<string>("PaymentProviders:Paystack:SecretKey"));
}).AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

builder.Services.AddHttpClient<InterswitchProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("PaymentProviders:Interswitch:BaseUrl") ?? "https://sandbox.interswitchng.com");
}).AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
