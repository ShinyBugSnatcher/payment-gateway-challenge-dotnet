using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

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
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PostPaymentResponse> GetPayment(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RejectedPaymentResponse),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status504GatewayTimeout)]
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
        catch (BankCommunicationException)
        {
            return Problem(
                title: "Acquiring bank communication failed.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (BankTimeoutException)
        {
            return Problem(
                title: "Acquiring bank request timed out.",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
    }
}