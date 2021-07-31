using HandwritingRecognition.Lib;
using HandwritingRecognition.Lib.SDCA;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System;

namespace HandwritingRecognition.SDCA.Training
{
	class Program
	{
		private static string outputModelPath { get; set; }
		private static string inputTrainingFilePath { get; set; }
		private static string inputTestFilePath { get; set; }
		private static IDataView trainingDataset;
		private static IDataView testDataset;
		private static MLContext mlContext;

		static void Main(string[] args)
		{
			outputModelPath = "MLModel.zip";
			// You can use relative paths here
			inputTrainingFilePath = @"D:\Github\HandwritingRecognition\HandwritingRecognition.SDCA.Training\Data\optdigits-train.csv";
			inputTestFilePath = @"D:\Github\HandwritingRecognition\HandwritingRecognition.SDCA.Training\Data\optdigits-val.csv";
			mlContext = new MLContext(1);
			Train();
		}

		static void Train()
		{
			try
			{
				// Step 1: Common data loading configuration
				trainingDataset = mlContext.Data.LoadFromTextFile(
					path: inputTrainingFilePath,
					columns: new[]
					{
						new TextLoader.Column(nameof(InputData.PixelValues), DataKind.Single,0,63),
						new TextLoader.Column("Number",DataKind.Single,64)
					},
					hasHeader: false,
					separatorChar: ','
					);

				testDataset = mlContext.Data.LoadFromTextFile(path: inputTestFilePath,
					columns: new[]
					{
						new TextLoader.Column(nameof(InputData.PixelValues), DataKind.Single, 0, 63),
						new TextLoader.Column("Number", DataKind.Single, 64)
					},
					hasHeader: false,
					separatorChar: ','
					);

				// Step 2: Common data process configuration with pipeline data transformations
				// Use in-memory cache for small/medium datasets to lower training time. Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets.
				var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "Number", keyOrdinality: ValueToKeyMappingEstimator.KeyOrdinality.ByValue).
					Append(mlContext.Transforms.Concatenate("Features", nameof(InputData.PixelValues)).AppendCacheCheckpoint(mlContext));

				// STEP 3: Set the training algorithm, then create and config the modelBuilder
				var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features");
				var trainingPipeline = dataProcessPipeline.Append(trainer).Append(mlContext.Transforms.Conversion.MapKeyToValue("Number", "Label"));

				// STEP 4: Train the model fitting to the DataSet

				Console.WriteLine("=============== Training the model ===============");
				ITransformer trainedModel = trainingPipeline.Fit(trainingDataset);

				Console.WriteLine("===== Evaluating Model's accuracy with Test data =====");
				var predictions = trainedModel.Transform(testDataset);
				var metrics = mlContext.MulticlassClassification.Evaluate(data: predictions, labelColumnName: "Number", scoreColumnName: "Score");

				ConsoleHelper.PrintMultiClassClassificationMetrics(trainer.ToString(), metrics);

				mlContext.Model.Save(trainedModel, trainingDataset.Schema, outputModelPath);

				Console.WriteLine("The model is saved to {0}", outputModelPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}
