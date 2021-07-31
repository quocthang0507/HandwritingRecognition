using HandwritingRecognition.Lib;
using HandwritingRecognition.Lib.DNN;
using HandwritingRecognition.Lib.SDCA;
using HandwritingRecognition.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HandwritingRecognition.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> logger;
		private readonly PredictionEnginePool<ImageDataInMemory, ImagePrediction> predictionEnginePoolDNN;
		private readonly PredictionEnginePool<InputData, OutputData> predictionEnginePoolSDCA;

		public HomeController(ILogger<HomeController> logger, PredictionEnginePool<ImageDataInMemory, ImagePrediction> predDNN, PredictionEnginePool<InputData, OutputData> predSDCA)
		{
			this.logger = logger;
			this.predictionEnginePoolDNN = predDNN;
			this.predictionEnginePoolSDCA = predSDCA;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult About()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		[HttpPost("api/Classify")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult UploadAndClassify(string base64Image, string classifierName = "DNN")
		{
			if (ModelState.IsValid && !string.IsNullOrEmpty(base64Image) && !string.IsNullOrEmpty(classifierName))
			{
				if (classifierName.Equals("DNN", StringComparison.OrdinalIgnoreCase))
				{
					byte[] imageData = ImageUtils.Base64ToByteArray(base64Image);
					if (imageData != null)
					{
						return ClassifyUsingDNN(imageData);
					}
				}
				else if (classifierName.Equals("SDCA", StringComparison.OrdinalIgnoreCase))
				{
					List<float> pixelValues = ImageUtils.GetPixels(base64Image.Replace("data:image/png;base64,", ""));
					if (pixelValues.Count > 0)
					{
						return ClassifyUsingSDCA(pixelValues);
					}
				}
			}
			return BadRequest("Missing parameter or can't process this request");
		}

		private static string FormatScores(IReadOnlyList<float> scores)
		{
			List<DigitResult> results = new();
			for (int i = 0; i < scores.Count; i++)
			{
				results.Add(new DigitResult(i, scores[i]));
			};
			return JsonConvert.SerializeObject(results);
		}

		private IActionResult ClassifyUsingDNN(byte[] imageData)
		{
			// Check that the image is valid
			if (!imageData.IsValidImage())
				return StatusCode(StatusCodes.Status415UnsupportedMediaType);

			logger.LogInformation("Start processing image...");

			// Measure execution time
			Stopwatch watch = Stopwatch.StartNew();

			// Set the specific image data into the ImageInputData type used in the DataView
			ImageDataInMemory imageInputData = new(imageData, null, null);

			// Predict code for provided image
			ImagePrediction prediction = predictionEnginePoolDNN.Predict(imageInputData);

			// Stop measuring time
			watch.Stop();
			long elapsedTime = watch.ElapsedMilliseconds;

			logger.LogInformation($"Image processed in {elapsedTime} miliseconds");

			// Predict the image's label with highest probability
			ImagePredictedLabelWithProbability bestPrediction = new()
			{
				PredictedLabel = prediction.Prediction,
				Results = FormatScores(prediction.Score),
				PredictionExecutionTime = elapsedTime
			};
			return Ok(bestPrediction);
		}

		private IActionResult ClassifyUsingSDCA(List<float> pixels)
		{
			logger.LogInformation("Start processing pixels...");

			// Measure execution time
			Stopwatch watch = Stopwatch.StartNew();

			InputData inputData = new()
			{
				PixelValues = pixels.ToArray()
			};

			// Predict code for provided image
			OutputData prediction = predictionEnginePoolSDCA.Predict(inputData);

			// Stop measuring time
			watch.Stop();
			long elapsedTime = watch.ElapsedMilliseconds;

			logger.LogInformation($"Image's pixels processed in {elapsedTime} miliseconds");

			// Predict the image's label with highest probability
			ImagePredictedLabelWithProbability bestPrediction = new()
			{
				PredictedLabel = prediction.Score.ToList().IndexOf(prediction.Score.Max()).ToString(),
				Results = FormatScores(prediction.Score),
				PredictionExecutionTime = elapsedTime
			};
			return Ok(bestPrediction);
		}
	}
}
