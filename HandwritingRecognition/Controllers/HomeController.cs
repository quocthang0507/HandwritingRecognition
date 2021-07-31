using HandwritingRecognition.Lib;
using HandwritingRecognition.Lib.DNN;
using HandwritingRecognition.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace HandwritingRecognition.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> logger;
		private readonly PredictionEnginePool<ImageDataInMemory, ImagePrediction> predictionEnginePool;

		public HomeController(ILogger<HomeController> logger, PredictionEnginePool<ImageDataInMemory, ImagePrediction> predictionEnginePool)
		{
			this.logger = logger;
			this.predictionEnginePool = predictionEnginePool;
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

		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult Upload(string base64Image)
		{
			if (!ModelState.IsValid || string.IsNullOrEmpty(base64Image))
			{
				return BadRequest(new { prediction = "-", dataset = string.Empty });
			}

			byte[] imageData = ImageUtils.Base64ToByteArray(base64Image);
			if (imageData == null)
			{
				return BadRequest(new { prediction = "-", dataset = string.Empty });
			}
			return Classify(imageData);
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

		private IActionResult Classify(byte[] imageData, string filename = null)
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
			ImagePrediction prediction = predictionEnginePool.Predict(imageInputData);

			// Stop measuring time
			watch.Stop();
			long elapsedTime = watch.ElapsedMilliseconds;

			logger.LogInformation($"Image processed in {elapsedTime} miliseconds");

			// Predict the image's label with highest probability
			ImagePredictedLabelWithProbability bestPrediction = new()
			{
				PredictedLabel = prediction.Prediction,
				Results = FormatScores(prediction.Score),
				PredictionExecutionTime = elapsedTime,
				ImageID = filename
			};
			return Ok(bestPrediction);
		}

	}
}
