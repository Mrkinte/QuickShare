using System.IO;
using System.Security.Cryptography;

namespace QuickShare.Helpers
{
    public class AesEncryptHelper
    {
        private const int MinEncryptedDataLength = 32;

        public static string Encrypt(long value, byte[] key)
        {
            ValidateKey(key);

            byte[] plainBytes = LongToBigEndianBytes(value);

            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();

                    byte[] cipherBytes = ms.ToArray();
                    byte[] result = new byte[iv.Length + cipherBytes.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

                    return Convert.ToBase64String(result).Replace("+", "-").Replace("/", "_");
                }
            }
        }

        public static long Decrypt(string encryptedString, byte[] key)
        {
            if (string.IsNullOrEmpty(encryptedString))
            {
                throw new ArgumentException("Encrypted string cannot be null or empty.", nameof(encryptedString));
            }

            byte[] encryptedData;
            try
            {
                encryptedData = Convert.FromBase64String(encryptedString.Replace("-", "+").Replace("_", "/"));
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid base64 string format.", nameof(encryptedString), ex);
            }

            if (encryptedData == null || encryptedData.Length < MinEncryptedDataLength)
            {
                throw new ArgumentException($"Encrypted data must be at least {MinEncryptedDataLength} bytes.", nameof(encryptedData));
            }

            ValidateKey(key);

            byte[] iv = new byte[16];
            int cipherLength = encryptedData.Length - iv.Length;
            if (cipherLength <= 0)
            {
                throw new ArgumentException("Invalid encrypted data: no ciphertext.", nameof(encryptedData));
            }

            byte[] cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);
            Buffer.BlockCopy(encryptedData, 16, cipherBytes, 0, cipherLength);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.FlushFinalBlock();

                    byte[] plainBytes = ms.ToArray();
                    if (plainBytes.Length != 8)
                    {
                        throw new CryptographicException("Decrypted data length is invalid. Expected 8 bytes.");
                    }

                    return BigEndianBytesToLong(plainBytes);
                }
            }
        }

        private static void ValidateKey(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "Key cannot be null.");
            }
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new ArgumentException("Key must be 16, 24, or 32 bytes long for AES-128/192/256.", nameof(key));
            }
        }

        private static byte[] LongToBigEndianBytes(long value)
        {
            return new byte[]
            {
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
        }

        private static long BigEndianBytesToLong(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8)
            {
                throw new ArgumentException("Byte array must contain at least 8 bytes.", nameof(bytes));
            }

            return (long)bytes[0] << 56 |
                   (long)bytes[1] << 48 |
                   (long)bytes[2] << 40 |
                   (long)bytes[3] << 32 |
                   (long)bytes[4] << 24 |
                   (long)bytes[5] << 16 |
                   (long)bytes[6] << 8 |
                   bytes[7];
        }
    }
}
