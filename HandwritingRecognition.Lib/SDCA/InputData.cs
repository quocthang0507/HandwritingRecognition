using Microsoft.ML.Data;

namespace HandwritingRecognition.Lib.SDCA
{
	public class InputData
	{
		[ColumnName("PixelValues")]
		[VectorType(64)]
		public float[] PixelValues;

		[LoadColumn(64)]
		public float Number;
	}
}
