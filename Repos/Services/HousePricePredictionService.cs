using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Propertease.Models;

namespace Propertease.Services
{
    public class HousePricePredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public HousePricePredictionService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiUrl = "http://localhost:5000";
        }

        public async Task<decimal> PredictHousePrice(HousePredictionRequest request)
        {
            try
            {
                // Convert the C# model to match exactly what the Flask API expects
                var apiRequest = new
                {
                    bedrooms = request.Bedrooms,
                    bathrooms = request.Bathrooms,
                    floors = request.Floors,
                    lotArea = request.LotArea,
                    houseArea = request.HouseArea,
                    builtYear = request.BuiltYear
                };

                var json = JsonConvert.SerializeObject(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiUrl}/predict", content);

                // Better error handling
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API returned status code {response.StatusCode}: {errorContent}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<PredictionResponse>(responseString);

                if (result == null || result.PredictedPrice <= 0)
                {
                    throw new Exception("Invalid prediction result received from API");
                }

                return result.PredictedPrice;
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Prediction API error: {ex.Message}");
                throw;
            }
        }
    }

    public class PredictionResponse
    {
        [JsonProperty("predicted_price")]
        public decimal PredictedPrice { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}