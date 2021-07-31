using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace HandwritingRecognition.Lib
{
	public class ImageUtils
	{
		/// <summary>
		/// Transform base64 string to byte[]
		/// </summary>
		/// <param name="base64String"></param>
		/// <returns></returns>
		public static byte[] Base64ToByteArray(string base64String)
		{
			if (base64String.StartsWith("data:image"))
				return Convert.FromBase64String(base64String.Split(',')[1]);
			return null;
		}

		public static List<float> GetPixels(string base64Image, int imageSize = 32, int areaSize = 4)
		{
			var imageBytes = ImageUtils.Base64ToByteArray(base64Image);

			var bitmap = new Bitmap(imageSize, imageSize);
			using (var graphic = Graphics.FromImage(bitmap))
			{
				graphic.Clear(Color.White);
				using var stream = new MemoryStream(imageBytes);
				var png = Image.FromStream(stream);
				graphic.DrawImage(png, 0, 0, imageSize, imageSize);
			}

			var result = new List<float>();
			for (int i = 0; i < imageSize; i += areaSize)
			{
				for (int j = 0; j < imageSize; j += areaSize)
				{
					var sum = 0;
					for (int k = i; k < i + areaSize; k++)
					{
						for (int l = j; l < j + areaSize; l++)
						{
							if (bitmap.GetPixel(l, k).Name != "ffffffff")
								sum++;
						}
					}
					result.Add(sum);
				}
			}
			return result;
		}
	}
}
