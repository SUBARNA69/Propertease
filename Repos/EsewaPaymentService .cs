using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PROPERTEASE.Services
{
    public interface IEsewaPaymentService
    {
        Task<string> GeneratePaymentUrl(string orderId, decimal amount, string productDetail, string successUrl, string failureUrl);
        Task<bool> VerifyPayment(string transactionId, string productId, decimal amount);
    }

    public class EsewaPaymentService : IEsewaPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _merchantId;
        private readonly string _esewaUrl;
        private readonly string _verificationUrl;

        public EsewaPaymentService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            // Get configuration values
            _merchantId = _configuration["Payment:eSewa:MerchantId"];
            _esewaUrl = _configuration["Payment:eSewa:PaymentUrl"];
            _verificationUrl = _configuration["Payment:eSewa:VerificationUrl"];
        }

        public Task<string> GeneratePaymentUrl(string orderId, decimal amount, string productDetail, string successUrl, string failureUrl)
        {
            var parameters = new Dictionary<string, string>
            {
                { "amt", amount.ToString() },
                { "pdc", "0" },  // Delivery charge
                { "psc", "0" },  // Service charge
                { "txAmt", "0" }, // Tax amount
                { "tAmt", amount.ToString() }, // Total amount
                { "pid", orderId },
                { "scd", _merchantId },
                { "su", successUrl },
                { "fu", failureUrl }
            };

            // Build the URL with parameters
            var url = _esewaUrl + "?" + string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

            return Task.FromResult(url);
        }

        public async Task<bool> VerifyPayment(string transactionId, string productId, decimal amount)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "amt", amount.ToString() },
                    { "rid", transactionId },
                    { "pid", productId },
                    { "scd", _merchantId }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync(_verificationUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the response based on eSewa's API documentation
                    return responseContent.Contains("success");
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