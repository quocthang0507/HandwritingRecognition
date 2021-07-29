using HandwritingRecognition.Lib;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Vision;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static Microsoft.ML.Transforms.ValueToKeyMappingEstimator;

namespace HandwritingRecognition.Traning
{
	class Program
	{
		private static string outputModelPath { get; set; }
		private static string inputFolderTraining { get; set; }
		private static string inputFolderTesting { get; set; }

		private const string KeyColumn = "LabelAsKey";
		private const string FeatureColumn = "ImageBytes";
		private const string ImagePathColumn = "ImagePath";
		private const string LabelColumn = "Label";
		private const string PredictedLabelColumn = "PredictedLabel";
		private static MLContext mlContext;
		private static IDataView trainingDataset;
		private static IDataView validationDataset;
		private static IDataView testDataset;
		private static TransformerChain<KeyToValueMappingTransformer> trainedModel;

		static void Main(string[] args)
		{
			outputModelPath = "MLModel.zip";
			inputFolderTraining = "";
			inputFolderTesting = "";
			mlContext = new MLContext(1);
			// RunPipeline();
		}

		static private void RunPipeline()
		{
			// 1., 2., 3., 4.
			PrepareDataset();

			// 5. Call pipeline
			EstimatorChain<KeyToValueMappingTransformer> pipeline = CreateCustomPipeline();

			// 6. Train/create the ML Model
			Console.WriteLine("*** Training the image classification model with DNN Transfer Learning on top of the selected pre-trained model/architecture ***");

			////////// Begin training
			Stopwatch watch = Stopwatch.StartNew();
			trainedModel = pipeline.Fit(trainingDataset);
			watch.Stop();
			////////// End training

			long ms = watch.ElapsedMilliseconds;
			Console.WriteLine($"Training with transfer learning took: {ms / 1000} seconds");

			// 8->7. Save the model to assets/outputs ML.NET .zip model file and TF .pb model file
			mlContext.Model.Save(trainedModel, trainingDataset.Schema, outputModelPath);
			Console.WriteLine($"Model saved to: {outputModelPath}");

			// 7->8. Get the quality metrics
			EvaluateModel();
		}

		/// <summary>
		/// Prepare dataset by loading from files, transforming and splitting
		/// </summary>
		static void PrepareDataset()
		{
			trainingDataset = TransformImagesToIDataView(inputFolderTraining);
			DataOperationsCatalog.TrainTestData validationTestSplit = mlContext.Data.TrainTestSplit(TransformImagesToIDataView(inputFolderTesting), 0.3);
			validationDataset = validationTestSplit.TrainSet;
			testDataset = validationTestSplit.TestSet;
		}

		private static IDataView TransformImagesToIDataView(string inputFolder)
		{
			// 2. Load the initial full image-set into an IDataView and shuffle so it'll be better balanced
			IEnumerable<ImageData> images = FileUtils.LoadImagesFromDirectory(inputFolder, true);
			IDataView dataset = mlContext.Data.LoadFromEnumerable(images);
			IDataView shuffledDataset = mlContext.Data.ShuffleRows(dataset);

			// 3. Load Images with in-memory type within the IDataView and Transform Labels to Keys (Categorical)
			IDataView transformedDataset = mlContext.Transforms.Conversion.
				MapValueToKey(KeyColumn, LabelColumn, keyOrdinality: KeyOrdinality.ByValue).
				// The outputColumnName should has same name in ImageDataInMemory
				Append(mlContext.Transforms.LoadRawImageBytes(FeatureColumn, inputFolder, ImagePathColumn)).
				Fit(shuffledDataset).
				Transform(shuffledDataset);
			return transformedDataset;
		}

		/// <summary>
		/// 5.1. (Optional) Define the model's training pipeline by using explicit hyper-parameters
		/// </summary>
		/// <param name="validationSet"></param>
		/// <returns></returns>
		private static EstimatorChain<KeyToValueMappingTransformer> CreateCustomPipeline()
		{
			ImageClassificationTrainer.Options options = new ImageClassificationTrainer.Options()
			{
				LabelColumnName = KeyColumn,
				// The feature column name should has same name in ImageDataInMemory
				FeatureColumnName = FeatureColumn,
				// Change the architecture to different DNN architecture
				Arch = ImageClassificationTrainer.Architecture.ResnetV250,
				// Number of training iterations
				Epoch = 200,
				// Number of samples to use for mini-batch training
				BatchSize = 10,
				LearningRate = 0.01f,
				MetricsCallback = (metrics) => Console.WriteLine(metrics),
			};
			options.ValidationSet = validationDataset;
			options.ValidationSet = testDataset;
			EstimatorChain<KeyToValueMappingTransformer> pipeline = mlContext.MulticlassClassification.Trainers.ImageClassification(options).
				Append(mlContext.Transforms.Conversion.MapKeyToValue(PredictedLabelColumn, PredictedLabelColumn));
			return pipeline;
		}

		private static void EvaluateModel()
		{
			Console.WriteLine("Making predictions in bulk for evaluating model's quality...");
			// Begin evaluating
			Stopwatch watch = Stopwatch.StartNew();
			IDataView predictionsDataView = trainedModel.Transform(testDataset);
			MulticlassClassificationMetrics metrics = mlContext.MulticlassClassification.Evaluate(predictionsDataView, labelColumnName: KeyColumn, predictedLabelColumnName: PredictedLabelColumn);
			ConsoleHelper.PrintMultiClassClassificationMetrics("TensorFlow DNN Transfer Learning", metrics);
			watch.Stop();
			// End evaluating
			long milliseconds = watch.ElapsedMilliseconds;
			Console.WriteLine($"Predicting and Evaluation took: {milliseconds / 1000} seconds");

			// Save confusion matrix metrics to file
			string confusionPath = Path.Combine(Directory.GetParent(outputModelPath).FullName, "ConfusionMatrix.csv");
			ConsoleHelper.Export_ConfusionMatrix(metrics.ConfusionMatrix, confusionPath, Path.GetFileNameWithoutExtension(outputModelPath));
		}
	}
}
