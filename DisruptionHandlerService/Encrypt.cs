using System.Security.Cryptography;
using System.Text;

namespace RECO.DistrubtionHandler_MS.DisruptionHandlerService
{
    public static class Encrypt
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("1122334455667788");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("8877665544332211");
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
    public static class AesEncryption
    {
        static string key = "RECO#344511223344556677885667788";
        private static readonly int KeySize = 256; // AES-256
        private static readonly int BlockSize = 128;

        // Encrypt a string
        public static string AesEncrypt(string plainText)
        {
            byte[] iv = new byte[BlockSize / 8]; // Initialization Vector with block size
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = Encoding.UTF8.GetBytes(key); // Convert key to bytes
                aes.IV = iv;
                aes.Mode = CipherMode.CBC; // Using CBC mode
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText); // Write encrypted data to stream
                        }
                        array = memoryStream.ToArray(); // Get the encrypted byte array
                    }
                }
            }

            return Convert.ToBase64String(array); // Return as a base64-encoded string
        }
        // Decrypt a string
        public static string AesDecrypt(string cipherText)
        {
            byte[] iv = new byte[BlockSize / 8]; // Initialization Vector with block size
            byte[] buffer = Convert.FromBase64String(cipherText); // Convert base64 string to byte array

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = Encoding.UTF8.GetBytes(key); // Convert key to bytes
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd(); // Return the decrypted string
                        }
                    }
                }
            }
        }
    }

}