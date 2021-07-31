using Microsoft.ML.Data;

namespace HandwritingRecognition.Lib.SDCA
{
	public class OutputData
	{
		[ColumnName("Score")]
		public float[] Score;
	}
}
