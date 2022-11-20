using System.Security.Cryptography;

namespace Maple_Server.Crypto;

public class CryptoProvider
{
    private const string RsaPublicKey = @"-----BEGIN PUBLIC KEY-----
        MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAt71LJQ5YrUWEJVphWSzy
        7LCNtWNYAsJUKb9DkWo886RntTIyVFndYoITAsT2+Uth87A+OmzG2ihwaUQH91Hz
        gpjKbAqcDq1fVkr9dS6tIi86/Nx3QH0slxN796ftzs2lyWtPuQ6JKQ3X0wSusEPt
        a2zy0frxxl9bs/t9CxuoCx/4KJCrhAwxgoOdKUA8TXfqMmQYYI9YTMvpZZJxeRI2
        GxcCX7hfxHZhv/M1xBQiM/Zfc0w7eQpOSFMFkGVKI4nxZSA+0tw9qu6gC78+WQGz
        0L++fjOdOEsdj/opIiCdxALDdnRd8dbMzPPdqjQvcOZikW0j1Xf+oAVxJWdahLw+
        HwIDAQAB
        -----END PUBLIC KEY-----";

    private const string xorKey = "xjCFQ58Pqd8KPNHp";

    private CryptoProvider() {}
    private static CryptoProvider? _instance;
    public static CryptoProvider Instance => _instance ??= new CryptoProvider();

    public byte[] XOR(byte[] data)
    {
        byte[] result = new byte[data.Length];
        
        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ (byte)xorKey[j]);

            j = ++j < xorKey.Length ? j : 0;
        }

        return result;
    }
    
    public byte[] RsaEncrypt(byte[] clearText)
    {
        using RSA rsa = RSA.Create();
        
        rsa.ImportFromPem(RsaPublicKey.ToCharArray());

        return rsa.Encrypt(clearText, RSAEncryptionPadding.OaepSHA1);
    }
    
    public byte[] AesEncrypt(byte[] clearText, byte[] key, byte[] iv)
    {
        using Aes aesAlgo = Aes.Create();
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
    
    public byte[] AesDecrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using var aesAlgo = Aes.Create();

        aesAlgo.KeySize = 256;
        aesAlgo.BlockSize = 128;
        aesAlgo.Mode = CipherMode.CBC;
        aesAlgo.Key = key;
        aesAlgo.IV = iv;

        var decryptor = aesAlgo.CreateDecryptor(aesAlgo.Key, aesAlgo.IV);

        using MemoryStream msDecrypt = new MemoryStream(cipherText);
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using MemoryStream msBuffer = new MemoryStream();

        csDecrypt.CopyTo(msBuffer);

        return msBuffer.ToArray();
    }
}