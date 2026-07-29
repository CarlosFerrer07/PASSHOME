namespace PasswordManager.Core.Interfaces;

public interface IDataKeyService
{
    void StoreKey(int userId, byte[] key);
    byte[]? GetKey(int userId);
    void RemoveKey(int userId);
}