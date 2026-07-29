using System.Collections.Concurrent;
using PasswordManager.Core.Interfaces;

namespace PasswordManager.Infrastructure.Services;

public class DataKeyService : IDataKeyService
{
    private static readonly ConcurrentDictionary<int, byte[]> _keys = new();

    public void StoreKey(int userId, byte[] key)
    {
        _keys[userId] = key;
    }

    public byte[]? GetKey(int userId)
    {
        _keys.TryGetValue(userId, out var key);
        return key;
    }

    public void RemoveKey(int userId)
    {
        _keys.TryRemove(userId, out _);
    }
}