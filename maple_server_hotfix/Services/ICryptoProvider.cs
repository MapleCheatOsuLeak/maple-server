using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace maple_server_hotfix.Services
{
    public interface ICryptoProvider
    {
        public byte[] AesDecrypt(byte[] cipherText, byte[] key, byte[] iv);
        public byte[] AesEncrypt(byte[] clearText, byte[] key, byte[] iv);

        public byte[] RsaEncrypt(byte[] data, out int sigLen);
    }

    public class CryptoProvider : ICryptoProvider
    {
        private static object _lock = new object();

        [DllImport("maple_crypto.dll")]
        private static extern IntPtr RSAEncrypt(IntPtr data, int size, ref int siglen, ref int outSize);


        public byte[] AesDecrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            byte[] outBuffer = new byte[cipherText.Length];

            using var aesAlgo = new RijndaelManaged();

            aesAlgo.KeySize = 256;
            aesAlgo.BlockSize = 128;
            aesAlgo.Mode = CipherMode.CBC;
            aesAlgo.Key = key;
            aesAlgo.IV = iv;

            var decryptor = aesAlgo.CreateDecryptor(aesAlgo.Key, aesAlgo.IV);

            using MemoryStream msDecrypt = new MemoryStream(cipherText);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

            csDecrypt.Read(outBuffer, 0, cipherText.Length - 32);

            return outBuffer;
        }

        public byte[] AesEncrypt(byte[] clearText, byte[] key, byte[] iv)
        {
            using RijndaelManaged aesAlgo = new RijndaelManaged();
            aesAlgo.KeySize = 256;
            aesAlgo.BlockSize = 128;
            aesAlgo.Mode = CipherMode.CBC;
            aesAlgo.Key = key;
            aesAlgo.IV = iv;

            ICryptoTransform encryptor = aesAlgo.CreateEncryptor(aesAlgo.Key, aesAlgo.IV);

            byte[] encrypted;
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    csEncrypt.Write(clearText);
                encrypted = msEncrypt.ToArray();
            }

            return encrypted;
        }

        public byte[] RsaEncrypt(byte[] data, out int sigLen)
        {
            byte[] result;
            int size = data.Length;
            sigLen = 0;

            lock (_lock)
            {
                unsafe
                {
                    fixed (byte* a = data)
                    {
                        int encSize = 0;
                        IntPtr crypted = RSAEncrypt((IntPtr)a, size, ref sigLen, ref encSize);
                        result = new byte[encSize];
                        Marshal.Copy(crypted, result, 0, encSize);
                    }
                }
            }
            return result;
        }
    }
}