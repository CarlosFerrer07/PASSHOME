namespace PasswordManager.Core.Interfaces;

public interface IEncryptionService
{
    byte[] Encrypt(string plainText, byte[] key, out byte[] iv);
    string Decrypt(byte[] cipherText, byte[] key, byte[] iv);
    byte[] GenerateKey();
    string HashPassword(string password, byte[] salt);
    bool VerifyPassword(string password, byte[] hash, byte[] salt);
    byte[] GenerateSalt();
    byte[] DeriveKeyFromPassword(string password, byte[] salt);
    string GeneratePassword(int length, bool includeUpper, bool includeLower, bool includeNumbers, bool includeSymbols);
}