using System.ComponentModel.DataAnnotations;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Services
{
    public class PaymentService
    {
        private readonly PaymentsRepository _paymentsRepository;
        private readonly PaymentRequestValidator _validator= new PaymentRequestValidator();
        public PaymentService(PaymentsRepository paymentsRepository)
        {
            _paymentsRepository = paymentsRepository;
        }

        public Task<PostPaymentResponse> ProcessPaymentAsync(
            PostPaymentRequest request)
        {
            var validationErrors = _validator.Validate(request);

            if (validationErrors.Count > 0)
            {
                // TODO: Return rejected payment result
                throw new NotImplementedException();
            }

            // TODO: Send payment to acquiring bank

            // TODO: Map bank response to payment response

            // TODO: Persist processed payment

            throw new NotImplementedException();
        }
    }
}

