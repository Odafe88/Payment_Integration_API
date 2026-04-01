using Microsoft.AspNetCore.Mvc;
using Payment_Integration_API.Models;
using Payment_Integration_API.Services;

namespace Payment_Integration_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("initiate")]
    public async Task<ActionResult<PaymentResult>> Initiate([FromBody] PaymentRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than 0.");

        request.Reference = string.IsNullOrWhiteSpace(request.Reference)
            ? Guid.NewGuid().ToString("N")
            : request.Reference;

        var result = await _paymentService.InitiateAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("verify")]
    public async Task<ActionResult<PaymentVerificationResult>> Verify([FromQuery] PaymentProvider provider, [FromQuery] string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return BadRequest("Reference is required.");

        var result = await _paymentService.VerifyAsync(provider, reference);
        return Ok(result);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] object content)
    {
        // TODO: implement per-provider webhook verification and event handling
        return Ok(new { received = true });
    }
}
