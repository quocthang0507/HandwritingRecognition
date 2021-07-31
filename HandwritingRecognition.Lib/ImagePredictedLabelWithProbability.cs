namespace HandwritingRecognition.Lib
{
	public class ImagePredictedLabelWithProbability
	{
		public string PredictedLabel { get; set; }
		public string Results { get; set; }
		public long PredictionExecutionTime { get; set; }
	}
}
