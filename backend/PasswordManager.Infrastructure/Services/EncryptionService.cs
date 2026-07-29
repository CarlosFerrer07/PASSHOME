using System.Security.Cryptography;
using System.Text;
using PasswordManager.Core.Interfaces;

namespace PasswordManager.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int Iterations = 100_000;

    public byte[] Encrypt(string plainText, byte[] key, out byte[] iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.GenerateIV();
        iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return cipherBytes;
    }

    public string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public byte[] GenerateKey()
    {
        var key = new byte[KeySize / 8];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            32);
    }

    public string HashPassword(string password, byte[] salt)
    {
        var hash = DeriveKeyFromPassword(password, salt);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        var computedHash = DeriveKeyFromPassword(password, salt);
        return CryptographicOperations.FixedTimeEquals(computedHash, hash);
    }

    public byte[] GenerateSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public string GeneratePassword(int length, bool includeUpper, bool includeLower, bool includeNumbers, bool includeSymbols)
    {
        var chars = new StringBuilder();
        if (includeUpper) chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        if (includeLower) chars.Append("abcdefghijklmnopqrstuvwxyz");
        if (includeNumbers) chars.Append("0123456789");
        if (includeSymbols) chars.Append("!@#$%^&*()_+-=[]{}|;:,.<>?");

        if (chars.Length == 0)
            chars.Append("abcdefghijklmnopqrstuvwxyz");

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }
        return new string(result);
    }
}