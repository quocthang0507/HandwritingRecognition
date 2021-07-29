using Microsoft.ML.Data;
using System.IO;

namespace HandwritingRecognition.Lib
{
	public class ImageDataInMemory
	{
		/// <summary>
		/// Bytes of image file
		/// </summary>
		public byte[] ImageBytes;

		/// <summary>
		/// Path to image file
		/// </summary>
		public string ImagePath { get; set; }

		/// <summary>
		/// Name of this image file
		/// </summary>
		public string Label { get; set; }

		public ImageDataInMemory(string imagePath, string label)
		{
			ImageBytes = File.ReadAllBytes(imagePath);
			ImagePath = imagePath;
			Label = label;
		}

		public ImageDataInMemory(byte[] imageBytes, string imagePath, string label)
		{
			this.ImageBytes = imageBytes;
			ImagePath = imagePath;
			Label = label;
		}
	}
}
