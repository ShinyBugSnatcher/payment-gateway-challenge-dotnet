using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services
{
    public class PaymentService
    {
        private readonly PaymentsRepository _paymentsRepository;

        public PaymentService(PaymentsRepository paymentsRepository)
        {
            _paymentsRepository = paymentsRepository;
        }

        public Task<PostPaymentResponse> ProcessPaymentAsync(
            PostPaymentRequest request)
        {
            // TODO: Validate payment request

            // TODO: Send payment to acquiring bank

            // TODO: Map bank response to payment response

            // TODO: Persist processed payment

            throw new NotImplementedException();
        }
    }
}

