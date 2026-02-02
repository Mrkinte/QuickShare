using System.IO;
using System.Security.Cryptography;

namespace QuickShare.Helpers
{
    public static class AesKeyManager
    {
        private const int KeySizeInBytes = 32;  // 256-bit key for AES-256.

        /// <summary>
        /// Generate a secure 32-byte AES key (AES-256).
        /// </summary>
        /// <returns>A 32-byte random key</returns>
        public static byte[] GenerateSecureKey()
        {
            byte[] key = new byte[KeySizeInBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// Save the key as a binary file.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="filePath"></param>
        public static void SaveKeyAsBinary(byte[] key, string filePath)
        {
            ValidateKey(key);
            File.WriteAllBytes(filePath, key);
        }

        /// <summary>
        /// Load the AES key from the binary file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>32-byte AES key</returns>
        public static byte[] LoadKeyFromBinary(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Key file not found: {filePath}.");

            byte[] key = File.ReadAllBytes(filePath);
            ValidateKey(key);
            return key;
        }

        /// <summary>
        /// Validate the AES key.
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateKey(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), "The key cannot be empty.");

            if (key.Length != KeySizeInBytes)
                throw new ArgumentException("Invalid key length.", nameof(key));
        }
    }
}
