using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

using static PaymentGateway.Api.Validation.PaymentRequestValidationHelper;


namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PaymentService _paymentService;
    public PaymentsController(PaymentsRepository paymentsRepository, PaymentService paymentService)
    {
        _paymentsRepository = paymentsRepository;
        _paymentService = paymentService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        return Ok(payment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RejectedPaymentResponse),StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
    [FromBody] PostPaymentRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(
                request,
                cancellationToken);

            return Ok(payment);
        }
        catch (PaymentValidationException exception)
        {
            return BadRequest(new RejectedPaymentResponse
            {
                Errors = exception.Errors
            });
        }
    }
}