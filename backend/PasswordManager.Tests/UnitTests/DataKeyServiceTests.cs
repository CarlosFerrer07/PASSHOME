using FluentAssertions;
using PasswordManager.Infrastructure.Services;

namespace PasswordManager.Tests.UnitTests;

public class DataKeyServiceTests
{
    private readonly DataKeyService _sut = new();

    [Fact]
    public void StoreKey_ShouldStoreKeySuccessfully()
    {
        var key = new byte[] { 1, 2, 3, 4, 5 };

        _sut.StoreKey(1, key);

        var retrieved = _sut.GetKey(1);
        retrieved.Should().BeEquivalentTo(key);
    }

    [Fact]
    public void GetKey_ShouldReturnNullForNonExistentUser()
    {
        var result = _sut.GetKey(999);

        result.Should().BeNull();
    }

    [Fact]
    public void GetKey_ShouldReturnStoredKey()
    {
        var key = new byte[] { 10, 20, 30 };
        _sut.StoreKey(42, key);

        var result = _sut.GetKey(42);

        result.Should().BeEquivalentTo(key);
    }

    [Fact]
    public void RemoveKey_ShouldRemoveStoredKey()
    {
        var key = new byte[] { 1, 2, 3 };
        _sut.StoreKey(1, key);

        _sut.RemoveKey(1);

        _sut.GetKey(1).Should().BeNull();
    }

    [Fact]
    public void RemoveKey_ShouldNotThrowForNonExistentUser()
    {
        Action act = () => _sut.RemoveKey(999);

        act.Should().NotThrow();
    }

    [Fact]
    public void StoreKey_ShouldOverwriteExistingKey()
    {
        var key1 = new byte[] { 1, 1, 1 };
        var key2 = new byte[] { 2, 2, 2 };
        _sut.StoreKey(1, key1);

        _sut.StoreKey(1, key2);

        _sut.GetKey(1).Should().BeEquivalentTo(key2);
    }

    [Fact]
    public void StoreKey_ShouldStoreMultipleUsersIndependently()
    {
        var key1 = new byte[] { 1 };
        var key2 = new byte[] { 2 };
        _sut.StoreKey(1, key1);
        _sut.StoreKey(2, key2);

        _sut.GetKey(1).Should().BeEquivalentTo(key1);
        _sut.GetKey(2).Should().BeEquivalentTo(key2);
    }

    [Fact]
    public void RemoveKey_ShouldOnlyRemoveSpecifiedUser()
    {
        var key1 = new byte[] { 1 };
        var key2 = new byte[] { 2 };
        _sut.StoreKey(1, key1);
        _sut.StoreKey(2, key2);

        _sut.RemoveKey(1);

        _sut.GetKey(1).Should().BeNull();
        _sut.GetKey(2).Should().BeEquivalentTo(key2);
    }
}
