using Microsoft.ML.Data;
using System;

namespace HandwritingRecognition.Lib
{
	public class ImagePrediction
	{
		// ColumnName attribute is used to change the column name from
		// its default value, which is the name of the field.
		[ColumnName("PredictedLabel")]
		public String Prediction { get; set; }

		[ColumnName("Score")]
		public float[] Score { get; set; }
	}
}
