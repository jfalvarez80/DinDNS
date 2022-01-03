using System.Security.Cryptography;
using System.Text;

namespace CleverNimbus.DinDNS.Common.Crypto
{
	public class AES
	{
		public static string Encrypt(string text, string key)
		{
			var cipher = CreateCipher(key);

			var cryptTransform = cipher.CreateEncryptor();
			byte[] plaintext = Encoding.UTF8.GetBytes(text);
			byte[] cipherText = cryptTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

			return Convert.ToBase64String(cipherText);
		}

		public static string Decrypt(string encryptedText, string key)
		{
			Aes cipher = CreateCipher(key);

			var cryptTransform = cipher.CreateDecryptor();
			byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
			byte[] plainBytes = cryptTransform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

			return Encoding.UTF8.GetString(plainBytes);
		}

		private static Aes CreateCipher(string keyBase64)
		{
			Aes cipher = Aes.Create();
			cipher.Mode = CipherMode.CBC;

			cipher.Padding = PaddingMode.ISO10126;
			cipher.Key = Convert.FromBase64String(keyBase64);

			return cipher;
		}
	}
}