using Microsoft.AspNetCore.Mvc;
using payment_gateway_nepal;

namespace Propertease.Controllers
{
    public class PaymentController : Controller
    {
        // Sandbox Credentials (FOR TESTING ONLY)
        private readonly string eSewa_SandboxKey = "8gBm/:&EnhH.1/q";

        // Set to false for production
        private readonly bool sandBoxMode = true;

        private string eSewaKey => sandBoxMode ? eSewa_SandboxKey : eSewa_SandboxKey;

        public async Task<IActionResult> PayWitheSewa()
        {
            PaymentManager paymentManager = new PaymentManager(
                PaymentMethod.eSewa,
                PaymentVersion.v2,
                sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                eSewaKey
            );
            string currentUrl = new Uri($"{Request.Scheme}://{Request.Host}").AbsoluteUri;
            // Replace these values with your actual order data
            dynamic request = new
            {
                Amount = 100,             // Your actual product amount
                TaxAmount = 10,           // Your actual tax amount
                TotalAmount = 110,        // Total including tax
                TransactionUuid = $"tx-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                ProductCode = sandBoxMode ? "EPAYTEST" : "YOUR_PRODUCT_CODE",
                ProductServiceCharge = 0,  // Your service charge if any
                ProductDeliveryCharge = 0, // Your delivery charge if any
                SuccessUrl = $"{currentUrl}/Payment/Success",  // Your actual success URL
                FailureUrl = $"{currentUrl}/Payment/Failure",  // Your actual failure URL
                SignedFieldNames = "total_amount,transaction_uuid,product_code"
            };
            var response = await paymentManager.InitiatePaymentAsync<ApiResponse>(request);
            return Redirect(response.data);
        }

        [Route("Payment/Success")]
        public async Task<IActionResult> VerifyEsewaPayment(string data)
        {
            PaymentManager paymentManager = new PaymentManager(
                PaymentMethod.eSewa,
                PaymentVersion.v2,
                sandBoxMode ? PaymentMode.Sandbox : PaymentMode.Production,
                eSewaKey
            );
            eSewaResponse response = await paymentManager.VerifyPaymentAsync<eSewaResponse>(data);
            if (!string.IsNullOrEmpty(nameof(response)) &&
                string.Equals(response.status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                // Handle successful payment
                // Update your order status, database, etc.
                ViewBag.Message = $"Payment successful: {response.transaction_code}, Amount: {response.total_amount}";
            }
            else
            {
                // Handle failed payment
                ViewBag.Message = "Payment verification failed";
            }
            return View();
        }
    }
}