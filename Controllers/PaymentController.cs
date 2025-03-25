using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Propertease.Models;

namespace Propertease.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ProperteaseDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ProperteaseDbContext context,
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // View Model for Payment Initiation
        public class EsewaPaymentViewModel
        {
            public string Amount { get; set; }
            public string TotalAmount { get; set; }
            public string TransactionUuid { get; set; }
            public string ProductCode { get; set; }
            public string ProductServiceCharge { get; set; }
            public string Signature { get; set; }
            public string SuccessUrl { get; set; }
            public string FailureUrl { get; set; }
            public string SignedFieldNames { get; set; }
            public string PaymentUrl { get; set; }
        }

        // Payment Response DTO
        public class EsewaPaymentResponse
        {
            public string TransactionCode { get; set; }
            public string Status { get; set; }
            public string TotalAmount { get; set; }
            public string TransactionUuid { get; set; }
            public string ProductCode { get; set; }
            public string SignedFieldNames { get; set; }
            public string Signature { get; set; }
        }

        // Initiate eSewa Payment
        [HttpGet]
        public async Task<IActionResult> InitiateEsewaPayment(int boostId)
        {
            _logger.LogInformation("Initiating eSewa payment for boost {BoostId}", boostId);

            try
            {
                // Retrieve boost record
                var boost = await _context.BoostedProperties
                    .FirstOrDefaultAsync(b => b.Id == boostId && b.PaymentStatus == "Pending");

                if (boost == null)
                {
                    _logger.LogWarning("Boost not found or already processed: {BoostId}", boostId);
                    return RedirectToAction("PaymentError", new { error = "invalid_boost" });
                }

                // Get eSewa configuration
                var secretKey = _configuration["Esewa:SecretKey"] ?? "8gBm/:&EnhH.1/q";
                var merchantCode = _configuration["Esewa:MerchantCode"] ?? "EPAYTEST";
                var paymentUrl = _configuration["Esewa:PaymentUrl"] ?? "https://rc-epay.esewa.com.np/api/epay/main/v2/form";

                // Prepare payment parameters
                var totalAmount = boost.Price.ToString("0.00", CultureInfo.InvariantCulture);
                var transactionUuid = Guid.NewGuid().ToString();

                // Generate signature
                var signatureString = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={merchantCode}";
                var signature = GenerateEsewaSignature(signatureString, secretKey);

                // Prepare callback URLs
                var successUrl = Url.Action("EsewaSuccess", "Payment", null, Request.Scheme);
                var failureUrl = Url.Action("EsewaFailure", "Payment", null, Request.Scheme);

                // Create view model
                var viewModel = new EsewaPaymentViewModel
                {
                    Amount = totalAmount,
                    TotalAmount = totalAmount,
                    TransactionUuid = transactionUuid,
                    ProductCode = merchantCode,
                    ProductServiceCharge = "0.00",
                    Signature = signature,
                    SuccessUrl = successUrl,
                    FailureUrl = failureUrl,
                    SignedFieldNames = "total_amount,transaction_uuid,product_code",
                    PaymentUrl = paymentUrl
                };

                return View("EsewaPaymentForm", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating eSewa payment for boost {BoostId}", boostId);
                return RedirectToAction("PaymentError", new { error = "payment_initiation_failed" });
            }
        }

        // eSewa Payment Success Callback
        [HttpGet]
        public async Task<IActionResult> EsewaSuccess([FromQuery] string data)
        {
            _logger.LogInformation("Received eSewa success callback with data: {Data}", data);

            try
            {
                // Decode base64 data
                var decodedData = Encoding.UTF8.GetString(Convert.FromBase64String(data));
                _logger.LogInformation("Decoded data: {DecodedData}", decodedData);

                // Deserialize payment response
                var paymentResponse = JsonSerializer.Deserialize<EsewaPaymentResponse>(decodedData);

                if (paymentResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize eSewa payment response");
                    return RedirectToAction("PaymentError", new { error = "invalid_response" });
                }

                // Validate payment response
                if (!ValidateEsewaPaymentResponse(paymentResponse))
                {
                    _logger.LogWarning("eSewa payment response validation failed");
                    return RedirectToAction("PaymentError", new { error = "validation_failed" });
                }

                // Find corresponding boosted property
                var boostedProperty = await _context.BoostedProperties
                    .FirstOrDefaultAsync(b => b.Id.ToString() == paymentResponse.TransactionUuid);

                if (boostedProperty == null)
                {
                    _logger.LogWarning("No boosted property found for transaction UUID: {TransactionUuid}",
                        paymentResponse.TransactionUuid);
                    return RedirectToAction("PaymentError", new { error = "boost_not_found" });
                }

                // Update payment status
                boostedProperty.PaymentStatus = paymentResponse.Status == "COMPLETE" ? "Paid" : "Failed";
                boostedProperty.TransactionCode = paymentResponse.TransactionCode;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment processed successfully for boost ID: {BoostId}", boostedProperty.Id);

                return RedirectToAction("PaymentConfirmation", new
                {
                    boostId = boostedProperty.Id,
                    transactionCode = paymentResponse.TransactionCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing eSewa payment success callback");
                return RedirectToAction("PaymentError", new { error = "processing_failed" });
            }
        }

        // eSewa Payment Failure Callback
        [HttpGet]
        public IActionResult EsewaFailure([FromQuery] string data)
        {
            _logger.LogWarning("eSewa payment failed. Received data: {Data}", data);
            return RedirectToAction("PaymentError", new { error = "payment_failed" });
        }

        // Payment Error Page
        public IActionResult PaymentError(string error)
        {
            var errorMessages = new Dictionary<string, string>
            {
                { "invalid_boost", "Invalid boost selected for payment." },
                { "payment_initiation_failed", "Failed to initiate payment. Please try again." },
                { "invalid_response", "Invalid payment response received." },
                { "validation_failed", "Payment validation failed." },
                { "boost_not_found", "Boost record not found." },
                { "payment_failed", "Payment was unsuccessful." },
                { "processing_failed", "An error occurred while processing the payment." }
            };

            ViewBag.ErrorMessage = errorMessages.ContainsKey(error)
                ? errorMessages[error]
                : "An unexpected error occurred during payment.";

            return View();
        }

        // Payment Confirmation Page
        public IActionResult PaymentConfirmation(int boostId, string transactionCode)
        {
            var boostedProperty = _context.BoostedProperties
                .Include(b => b.Property)
                .FirstOrDefault(b => b.Id == boostId);

            if (boostedProperty == null)
            {
                return RedirectToAction("PaymentError", new { error = "boost_not_found" });
            }

            var viewModel = new PaymentConfirmationViewModel
            {
                BoostId = boostId,
                TransactionCode = transactionCode,
                Amount = boostedProperty.Price,
                PaymentDate = DateTime.UtcNow,
                IsSuccessful = boostedProperty.PaymentStatus == "Paid"
            };

            return View(viewModel);
        }

        // Signature Generation Method
        private string GenerateEsewaSignature(string signatureString, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Payment Response Validation
        private bool ValidateEsewaPaymentResponse(EsewaPaymentResponse response)
        {
            // Comprehensive validation checks
            if (response == null)
            {
                _logger.LogWarning("Payment response is null");
                return false;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(response.TransactionCode) ||
                string.IsNullOrEmpty(response.Status) ||
                string.IsNullOrEmpty(response.TotalAmount))
            {
                _logger.LogWarning("Missing required payment response fields");
                return false;
            }

            // Verify product code
            if (response.ProductCode != "EPAYTEST")
            {
                _logger.LogWarning("Invalid product code: {ProductCode}", response.ProductCode);
                return false;
            }

            // Verify payment status
            if (response.Status != "COMPLETE")
            {
                _logger.LogWarning("Payment status is not COMPLETE: {Status}", response.Status);
                return false;
            }

            return true;
        }
    }

    // View Model for Payment Confirmation
    public class PaymentConfirmationViewModel
    {
        public int BoostId { get; set; }
        public string TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PropertyName { get; set; }
        public string BoostType { get; set; }
        public bool IsSuccessful { get; set; } = true;

        // Formatted properties
        public string FormattedAmount => Amount.ToString("C2");
        public string FormattedPaymentDate => PaymentDate.ToString("f");
    }
}