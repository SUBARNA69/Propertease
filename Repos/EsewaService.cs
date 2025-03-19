using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Propertease.Repos
{
    public class EsewaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _merchantId;
        private readonly string _secretKey;
        private readonly bool _isProduction;
        private readonly string _paymentUrl;
        private readonly string _verificationUrl;

        public EsewaService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            // Read values from configuration
            _merchantId = _configuration["Esewa:MerchantId"];
            _secretKey = _configuration["Esewa:SecretKey"];
            _isProduction = _configuration.GetValue<bool>("Esewa:IsProduction");
            _paymentUrl = _configuration["Esewa:PaymentUrl"];
            _verificationUrl = _configuration["Esewa:VerificationUrl"];
        }

        public string GeneratePaymentUrl(string orderId, decimal amount, string successUrl, string failureUrl)
        {
            // Calculate tax and total amount (you can adjust this based on your requirements)
            decimal taxAmount = 0; // Set to 0 or calculate as needed
            decimal totalAmount = amount; // Total amount should include tax if applicable

            // Generate a unique transaction UUID
            string transactionUuid = orderId;

            // Set product code (EPAYTEST for testing, your actual code for production)
            string productCode = _merchantId;

            // Fields to be signed
            string signedFieldNames = "total_amount,transaction_uuid,product_code";

            // Generate signature
            string dataToSign = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={productCode}";
            string signature = GenerateHmacSignature(dataToSign, _secretKey);

            // Build the form HTML
            var formHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Redirecting to eSewa...</title>
    <script>
        window.onload = function() {{
            document.getElementById('esewaForm').submit();
        }}
    </script>
</head>
<body>
    <form id='esewaForm' action='{_paymentUrl}' method='POST'>
        <input type='hidden' name='amount' value='{amount}'>
        <input type='hidden' name='tax_amount' value='{taxAmount}'>
        <input type='hidden' name='total_amount' value='{totalAmount}'>
        <input type='hidden' name='transaction_uuid' value='{transactionUuid}'>
        <input type='hidden' name='product_code' value='{productCode}'>
        <input type='hidden' name='product_service_charge' value='0'>
        <input type='hidden' name='product_delivery_charge' value='0'>
        <input type='hidden' name='success_url' value='{successUrl}'>
        <input type='hidden' name='failure_url' value='{failureUrl}'>
        <input type='hidden' name='signed_field_names' value='{signedFieldNames}'>
        <input type='hidden' name='signature' value='{signature}'>
    </form>
    <div style='text-align:center; margin-top:50px;'>
        <h2>Redirecting to eSewa Payment Gateway...</h2>
        <p>Please wait, you will be redirected automatically.</p>
    </div>
</body>
</html>";

            return formHtml;
        }

        private string GenerateHmacSignature(string data, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }

        public async Task<bool> VerifyPayment(string referenceId, string productId, decimal amount)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "amt", amount.ToString() },
                    { "rid", referenceId },
                    { "pid", productId },
                    { "scd", _merchantId }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(_verificationUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent.Contains("Success");
                }

                return false;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error verifying eSewa payment: {ex.Message}");
                return false;
            }
        }
    }
}