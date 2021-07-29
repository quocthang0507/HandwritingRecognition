using HandwritingRecognition.Models;
using HandwritingRecognitionML.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace HandwritingRecognition.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly PredictionEnginePool<ModelInput, ModelOutput> _predictionEnginePool;
		private const int SizeOfImage = 32;
		private const int SizeOfArea = 4;

		public HomeController(ILogger<HomeController> logger, PredictionEnginePool<ModelInput, ModelOutput> predictionEnginePool)
		{
			_logger = logger;
			_predictionEnginePool = predictionEnginePool;
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
			if (string.IsNullOrEmpty(base64Image))
			{
				return BadRequest(new { prediction = "-", dataset = string.Empty });
			}
			var pixelValues = GetPixelValuesFromImage(base64Image.Replace("data:image/png;base64,", ""));
			var input = new ModelInput { PixelValues = pixelValues.ToArray() };
			var result = _predictionEnginePool.Predict(modelName: HWRModel.Name, example: input);
			_logger.LogInformation($"Number {result.Prediction} is returned.");
			return Ok(new
			{
				prediction = result.Prediction,
				scores = FormatScores(result.Score),
				pixelValues = string.Join(",", pixelValues)
			});
		}

		private static List<float> GetPixelValuesFromImage(string base64Image)
		{
			var imageBytes = Convert.FromBase64String(base64Image).ToArray();

			// resize the original image and save it as bitmap
			var bitmap = new Bitmap(SizeOfImage, SizeOfImage);
			using (var g = Graphics.FromImage(bitmap))
			{
				g.Clear(Color.White);
				using var stream = new MemoryStream(imageBytes);
				var png = Image.FromStream(stream);
				g.DrawImage(png, 0, 0, SizeOfImage, SizeOfImage);
			}

			// aggregate pixels in 4X4 area --> 'result' is a list of 64 integers
			var result = new List<float>();
			for (var i = 0; i < SizeOfImage; i += SizeOfArea)
			{
				for (var j = 0; j < SizeOfImage; j += SizeOfArea)
				{
					var sum = 0;        // 'sum' is in the range of [0,16].
					for (var k = i; k < i + SizeOfArea; k++)
					{
						for (var l = j; l < j + SizeOfArea; l++)
						{
							if (bitmap.GetPixel(l, k).Name != "ffffffff") sum++;
						}
					}
					result.Add(sum);
				}
			}

			return result;
		}

		private static string FormatScores(IReadOnlyList<float> scores)
		{
			// order is: 0,7,4,6,2,5,8,1,9,3 (the order of labels appear in the training data set)
			List<DigitResult> results = new()
			{
				new DigitResult(0, scores[0]),
				new DigitResult(1, scores[7]),
				new DigitResult(2, scores[4]),
				new DigitResult(3, scores[9]),
				new DigitResult(4, scores[2]),
				new DigitResult(5, scores[5]),
				new DigitResult(6, scores[3]),
				new DigitResult(7, scores[1]),
				new DigitResult(8, scores[6]),
				new DigitResult(9, scores[8])
			};
			return JsonConvert.SerializeObject(results);
		}
	}
}
