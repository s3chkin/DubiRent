using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DubiRent.Data;
using DubiRent.Data.Models;
using DubiRent.Services;
using Stripe;
using Stripe.Checkout;

namespace DubiRent.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IStripeService _stripeService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ApplicationDbContext db,
            IStripeService stripeService,
            UserManager<AppUser> userManager,
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            this.db = db;
            _stripeService = stripeService;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: Create Payment Session
        [Authorize]
        public async Task<IActionResult> CreatePayment(int propertyId)
        {
            var property = await db.Properties
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null)
            {
                TempData["Error"] = "Property not found.";
                return RedirectToAction("Properties", "Property");
            }

            if (property.Status != PropertyStatus.Available)
            {
                TempData["Error"] = "This property is not available for payment.";
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // Redirect to login
            }

            // Check if user is admin - admins cannot make payments
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Administrators cannot make payments.";
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }

            // Check if user has an approved viewing request for this property
            var hasApprovedRequest = await db.ViewingRequests
                .AnyAsync(vr => vr.PropertyId == propertyId 
                    && vr.UserId == userId 
                    && vr.Status == ViewingRequestStatus.Approved);

            if (!hasApprovedRequest)
            {
                TempData["Error"] = "You need an approved viewing request to proceed with payment.";
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }

            var amount = property.PricePerMonth;
            // Get currency from configuration, default to USD
            var currency = _configuration["Stripe:Currency"]?.ToLower() ?? "usd";

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/Property/Details/{propertyId}";

            try
            {
                var session = await _stripeService.CreateCheckoutSessionAsync(
                    propertyId,
                    amount,
                    currency,
                    userId,
                    successUrl,
                    cancelUrl
                );

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment session for property {propertyId}");
                TempData["Error"] = "An error occurred while processing your payment. Please try again.";
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }
        }

        // GET: Payment Success
        [Authorize]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                TempData["Error"] = "Invalid payment session.";
                return RedirectToAction("Properties", "Property");
            }

            try
            {
                var session = await _stripeService.GetCheckoutSessionAsync(session_id);

                if (session.PaymentStatus != "paid")
                {
                    TempData["Error"] = "Payment was not completed successfully.";
                    return RedirectToAction("Properties", "Property");
                }

                // Extract metadata
                var propertyIdStr = session.Metadata?.GetValueOrDefault("propertyId");
                var userId = session.Metadata?.GetValueOrDefault("userId");

                if (string.IsNullOrEmpty(propertyIdStr) || !int.TryParse(propertyIdStr, out int propertyId))
                {
                    TempData["Error"] = "Invalid payment information.";
                    return RedirectToAction("Properties", "Property");
                }

                // Check if payment already exists
                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == session_id);

                if (existingPayment == null)
                {
                    // Create payment record
                    var payment = new Payment
                    {
                        UserId = userId ?? _userManager.GetUserId(User),
                        PropertyId = propertyId,
                        Amount = (decimal)session.AmountTotal! / 100, // Convert from cents
                        Currency = session.Currency?.ToUpper() ?? "USD",
                        PaymentStatus = PaymentStatus.Completed,
                        PaymentProvider = "Stripe",
                        TransactionId = session_id,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();

                    _logger.LogInformation($"Payment recorded: {payment.Id} for property {propertyId}");
                }

                TempData["Success"] = "Payment completed successfully!";
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment success: {ex.Message}");
                TempData["Error"] = "An error occurred while processing your payment.";
                return RedirectToAction("Properties", "Property");
            }
        }

        // GET: Payment Cancel
        [Authorize]
        public IActionResult Cancel()
        {
            TempData["Info"] = "Payment was cancelled.";
            return RedirectToAction("Properties", "Property");
        }

        // POST: Stripe Webhook
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];

            try
            {
                var webhookSecret = HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["Stripe:WebhookSecret"];

                if (string.IsNullOrEmpty(webhookSecret))
                {
                    _logger.LogWarning("Stripe webhook secret is not configured.");
                    return BadRequest();
                }

                var stripeEvent = Stripe.EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    webhookSecret
                );

                // Handle the event
                switch (stripeEvent.Type)
                {
                    case Events.CheckoutSessionCompleted:
                        var session = stripeEvent.Data.Object as Session;
                        await HandleCheckoutSessionCompleted(session);
                        break;

                    case Events.PaymentIntentSucceeded:
                        var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                        await HandlePaymentIntentSucceeded(paymentIntent);
                        break;

                    case Events.PaymentIntentPaymentFailed:
                        var failedPayment = stripeEvent.Data.Object as Stripe.PaymentIntent;
                        await HandlePaymentIntentFailed(failedPayment);
                        break;

                    default:
                        _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
                        break;
                }

                return Ok();
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, $"Stripe webhook error: {ex.Message}");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing webhook: {ex.Message}");
                return StatusCode(500);
            }
        }

        private async Task HandleCheckoutSessionCompleted(Session session)
        {
            try
            {
                if (session.PaymentStatus != "paid")
                    return;

                var propertyIdStr = session.Metadata?.GetValueOrDefault("propertyId");
                var userId = session.Metadata?.GetValueOrDefault("userId");

                if (string.IsNullOrEmpty(propertyIdStr) || !int.TryParse(propertyIdStr, out int propertyId))
                {
                    _logger.LogWarning($"Invalid property ID in checkout session: {session.Id}");
                    return;
                }

                // Check if payment already exists
                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == session.Id);

                if (existingPayment == null)
                {
                    var payment = new Payment
                    {
                        UserId = userId ?? "",
                        PropertyId = propertyId,
                        Amount = (decimal)session.AmountTotal! / 100,
                        Currency = session.Currency?.ToUpper() ?? "USD",
                        PaymentStatus = PaymentStatus.Completed,
                        PaymentProvider = "Stripe",
                        TransactionId = session.Id,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();

                    _logger.LogInformation($"Payment created from webhook: {payment.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling checkout session completed: {ex.Message}");
            }
        }

        private async Task HandlePaymentIntentSucceeded(Stripe.PaymentIntent paymentIntent)
        {
            try
            {
                var propertyIdStr = paymentIntent.Metadata?.GetValueOrDefault("propertyId");
                var userId = paymentIntent.Metadata?.GetValueOrDefault("userId");

                if (string.IsNullOrEmpty(propertyIdStr) || !int.TryParse(propertyIdStr, out int propertyId))
                {
                    return;
                }

                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == paymentIntent.Id);

                if (existingPayment == null)
                {
                    var payment = new Payment
                    {
                        UserId = userId ?? "",
                        PropertyId = propertyId,
                        Amount = (decimal)paymentIntent.Amount / 100,
                        Currency = paymentIntent.Currency.ToUpper(),
                        PaymentStatus = PaymentStatus.Completed,
                        PaymentProvider = "Stripe",
                        TransactionId = paymentIntent.Id,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();
                }
                else
                {
                    existingPayment.PaymentStatus = PaymentStatus.Completed;
                    existingPayment.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling payment intent succeeded: {ex.Message}");
            }
        }

        private async Task HandlePaymentIntentFailed(Stripe.PaymentIntent paymentIntent)
        {
            try
            {
                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == paymentIntent.Id);

                if (existingPayment != null)
                {
                    existingPayment.PaymentStatus = PaymentStatus.Failed;
                    existingPayment.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling payment intent failed: {ex.Message}");
            }
        }
    }
}

