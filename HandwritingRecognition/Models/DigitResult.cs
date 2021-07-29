namespace HandwritingRecognition.Models
{
	public class DigitResult
	{
		public DigitResult(int digit, float score)
		{
			Digit = digit;
			Score = score;
		}

		public int Digit { get; set; }
		public float Score { get; set; }
	}
}
