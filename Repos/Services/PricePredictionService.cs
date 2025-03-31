using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Propertease.Models;


public class PricePredictionService
{
    private readonly InferenceSession _session;
    private readonly ILogger<PricePredictionService> _logger;

    public PricePredictionService(IWebHostEnvironment env, ILogger<PricePredictionService> logger)
    {
        _logger = logger;
        try
        {
            // Load the ONNX model with proper error handling
            var modelPath = Path.Combine(env.WebRootPath, "aimodels", "house_price_model.onnx");

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"ONNX model file not found at path: {modelPath}");
            }

            _session = new InferenceSession(modelPath);

            // Validate input/output names
            var inputNames = _session.InputMetadata.Keys.ToList();
            _logger.LogInformation($"Model loaded successfully. Input names: {string.Join(", ", inputNames)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ONNX model");
            throw; // Rethrow to fail fast on startup
        }
    }

    public float PredictPrice(PropertyInputFeatures inputFeatures)
    {
        try
        {
            // Validate inputs before processing
            if (inputFeatures == null)
            {
                throw new ArgumentNullException(nameof(inputFeatures), "Input features cannot be null");
            }

            // Convert all nullable inputs to float with default values for nulls
            float houseArea = (float)(inputFeatures.HouseArea ?? 0);
            float bedrooms = (float)(inputFeatures.Bedrooms ?? 0);
            float bathrooms = (float)(inputFeatures.Bathrooms ?? 0);
            float lotArea = (float)(inputFeatures.LotArea ?? 0);
            float floors = (float)(inputFeatures.Floors ?? 0);
            float latitude = (float)(inputFeatures.Latitude ?? 0);
            float longitude = (float)(inputFeatures.Longitude ?? 0);

            // IMPORTANT: Make sure this matches how your model was trained
            // If your model expects the actual year, use this:
            float builtYear = inputFeatures.BuiltYear.Year;
            // If your model expects years since built, use this:
            // float builtYear = (float)(DateTime.Now.Year - inputFeatures.BuiltYear.Year);

            // Log input values for debugging
            _logger.LogInformation($"Prediction inputs: HouseArea={houseArea}, Bedrooms={bedrooms}, " +
                $"Bathrooms={bathrooms}, LotArea={lotArea}, Floors={floors}, " +
                $"Lat={latitude}, Long={longitude}, BuiltYear={builtYear}");

            // Prepare input tensor with explicit float conversion
            var inputData = new float[]
            {
                houseArea,
                bedrooms,
                bathrooms,
                lotArea,
                floors,
                latitude,
                longitude,
                builtYear
            };

            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, inputData.Length });

            // Get the actual input name from the model metadata
            string inputName = _session.InputMetadata.Keys.First();

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            // Run inference with error handling
            using var results = _session.Run(inputs);

            // Get the actual output name
            string outputName = _session.OutputMetadata.Keys.First();
            var output = results.First(r => r.Name == outputName).AsTensor<float>();

            float prediction = output[0];
            _logger.LogInformation($"Prediction result: {prediction}");

            return prediction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during price prediction");
            throw; // Rethrow to be handled by the controller
        }
    }

}