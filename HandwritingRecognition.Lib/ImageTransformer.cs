using System;

namespace HandwritingRecognition.Lib
{
	public class ImageTransformer
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
	}
}
