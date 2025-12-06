using Stripe;
using Stripe.Checkout;

namespace DubiRent.Services
{
    public interface IStripeService
    {
        Task<Session> CreateCheckoutSessionAsync(int propertyId, decimal amount, string currency, string userId, string successUrl, string cancelUrl);
        Task<Session> GetCheckoutSessionAsync(string sessionId);
        Task<PaymentIntent> CreatePaymentIntentAsync(int propertyId, decimal amount, string currency, string userId);
        Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
    }

    public class StripeService : IStripeService
    {
        private readonly ILogger<StripeService> _logger;
        private readonly IConfiguration _configuration;

        public StripeService(ILogger<StripeService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Set Stripe API key
            var secretKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(secretKey))
            {
                StripeConfiguration.ApiKey = secretKey;
            }
        }

        public async Task<Session> CreateCheckoutSessionAsync(int propertyId, decimal amount, string currency, string userId, string successUrl, string cancelUrl)
        {
            try
            {
                var amountInSmallestUnit = (long)(amount * 100); // Convert to cents/pennies

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                    {
                        "card"
                    },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = currency.ToLower(),
                                UnitAmount = amountInSmallestUnit,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Property Rental - ID: {propertyId}",
                                    Description = "Property rental payment"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    ClientReferenceId = $"{propertyId}_{userId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "propertyId", propertyId.ToString() },
                        { "userId", userId },
                        { "amount", amount.ToString("F2") },
                        { "currency", currency }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation($"Stripe checkout session created: {session.Id} for property {propertyId}");
                return session;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Stripe error creating checkout session: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating Stripe checkout session: {ex.Message}");
                throw;
            }
        }

        public async Task<Session> GetCheckoutSessionAsync(string sessionId)
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);
                return session;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Stripe error getting session: {ex.Message}");
                throw;
            }
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(int propertyId, decimal amount, string currency, string userId)
        {
            try
            {
                var amountInSmallestUnit = (long)(amount * 100);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInSmallestUnit,
                    Currency = currency.ToLower(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "propertyId", propertyId.ToString() },
                        { "userId", userId }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                _logger.LogInformation($"Stripe payment intent created: {paymentIntent.Id} for property {propertyId}");
                return paymentIntent;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Stripe error creating payment intent: {ex.Message}");
                throw;
            }
        }

        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);
                return paymentIntent;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Stripe error getting payment intent: {ex.Message}");
                throw;
            }
        }
    }
}

