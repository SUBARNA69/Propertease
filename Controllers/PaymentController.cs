using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Propertease.Models;
using Propertease.Repos;

namespace Propertease.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ProperteaseDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(ProperteaseDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Helper method to generate HMAC SHA256 signature and return it as Base64 string
        private string GenerateSignature(string totalAmount, string transactionUuid, string productCode, string secretKey)
        {
            // Create the data string in the required format. Adjust the format if needed.
            string data = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={productCode}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPayment(int boostedPropertyId)
        {
            // Get the boosted property details
            var boostedProperty = await _context.BoostedProperties
                .Include(bp => bp.Property)
                .FirstOrDefaultAsync(bp => bp.Id == boostedPropertyId);

            if (boostedProperty == null)
            {
                return NotFound();
            }

            // Verify property ownership
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var property = await _context.properties
                .FirstOrDefaultAsync(p => p.Id == boostedProperty.PropertyId && p.SellerId == userId);

            if (property == null)
            {
                return Forbid();
            }

            // Create a new payment record. Set ReferenceId to empty since it will be updated later.
            var payment = new Payment
            {
                BoostedPropertyId = boostedPropertyId,
                Amount = (decimal)boostedProperty.Price,
                PaymentMethod = "eSewa",
                Status = "Pending",
                TransactionId = $"BOOST-{boostedPropertyId}-{DateTime.UtcNow.Ticks}",
                ReferenceId = string.Empty
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Read configuration values for eSewa integration
            string merchantId =  "EPAYTEST"; // Use real merchantId in production
            string productCode =  "EPAYTEST"; // Usually provided by eSewa
            string secretKey =  "8gBm/:&EnhH.1/q"; // Example secret for UAT
            string esewaUrl =  "https://rc-epay.esewa.com.np/api/epay/main/v2/form";

            // Make sure these values are set correctly
            string amount = payment.Amount.ToString("F2"); // The actual product amount
            string taxAmount = "0.00"; // Use "0.00" instead of "0"
            string serviceCharge = "0.00"; // Use "0.00" instead of "0"
            string deliveryCharge = "0.00"; // Use "0.00" instead of "0"

            // Calculate total amount
            decimal totalAmountDecimal = payment.Amount +
                                        decimal.Parse(taxAmount) +
                                        decimal.Parse(serviceCharge) +
                                        decimal.Parse(deliveryCharge);
            string totalAmount = totalAmountDecimal.ToString("F2");

            // Set up the transaction UUID
            string transactionUuid = payment.TransactionId;

            // Define the signed field names in the correct order
            string signedFieldNames = "total_amount,transaction_uuid,product_code";

            // Generate the signature based on required fields
            string signature = GenerateSignature(totalAmount, transactionUuid, productCode, secretKey);

            var esewaData = new
            {
                MerchantId = merchantId,
                Amount = amount,
                TaxAmount = taxAmount,
                ServiceCharge = serviceCharge,
                DeliveryCharge = deliveryCharge,
                TotalAmount = totalAmount,
                TransactionUuid = transactionUuid,
                ProductCode = productCode,
                SuccessUrl = Url.Action("PaymentSuccess", "Payment", null, Request.Scheme),
                FailureUrl = Url.Action("PaymentFailure", "Payment", null, Request.Scheme),
                SignedFieldNames = signedFieldNames,
                Signature = signature
            };


            // Pass the payment data to the view to be used in the form post to eSewa
            ViewBag.ESewaData = esewaData;
            ViewBag.PaymentId = payment.Id;
            ViewBag.ESewaUrl = esewaUrl;
            ViewBag.IsTestMode = true; // Set to false in production

            return View(boostedProperty);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string pid, string amt, string refId)
        {
            // Validate incoming parameters (consider enhancing validation)
            if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(amt) || string.IsNullOrEmpty(refId))
            {
                return BadRequest("Invalid payment details received.");
            }

            // Find the payment by transaction id (pid)
            var payment = await _context.Payments
                .Include(p => p.BoostedProperty)
                .FirstOrDefaultAsync(p => p.TransactionId == pid);

            if (payment == null)
            {
                return NotFound();
            }

            // Here, you should decode and verify the response if it is Base64 encoded.
            // For this example, we assume the payment is verified via an external API call.
            bool isVerified = await VerifyESewaPayment(pid, amt, refId);

            DateTime nepalNow = DateTime.UtcNow.ToNepalTime(); // Assuming you have an extension method ToNepalTime()

            if (isVerified)
            {
                // Update payment status and details
                payment.Status = "Success";
                payment.ReferenceId = refId;
                payment.UpdatedAt = nepalNow;

                // Activate the boost
                var boostedProperty = payment.BoostedProperty;
                boostedProperty.IsActive = true;
                boostedProperty.StartTime = nepalNow;
                boostedProperty.EndTime = nepalNow.AddHours(boostedProperty.Hours);

                await _context.SaveChangesAsync();
                return RedirectToAction("PaymentConfirmation", new { id = payment.Id });
            }
            else
            {
                payment.Status = "Failed";
                payment.UpdatedAt = nepalNow;
                await _context.SaveChangesAsync();
                return RedirectToAction("PaymentFailure", new { id = payment.Id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentFailure(string pid = null, int id = 0)
        {
            Payment payment = null;

            if (!string.IsNullOrEmpty(pid))
            {
                payment = await _context.Payments
                    .Include(p => p.BoostedProperty)
                    .FirstOrDefaultAsync(p => p.TransactionId == pid);
            }
            else if (id > 0)
            {
                payment = await _context.Payments
                    .Include(p => p.BoostedProperty)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }

            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Failed";
            payment.UpdatedAt = DateTime.UtcNow.ToNepalTime();
            await _context.SaveChangesAsync();
            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentConfirmation(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.BoostedProperty)
                .ThenInclude(bp => bp.Property)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }
            return View(payment);
        }

        // Helper method to verify eSewa payment
        private async Task<bool> VerifyESewaPayment(string pid, string amt, string refId)
        {
            // In a real implementation, call eSewa's transaction verification API and validate the response.
            // For testing purposes, we assume verification is successful.
            await Task.Delay(10);
            return true;
        }
    }
}
