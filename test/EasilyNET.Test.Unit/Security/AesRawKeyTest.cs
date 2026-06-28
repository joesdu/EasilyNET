using System.Security.Cryptography;
using System.Text;
using EasilyNET.Security;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// Tests for the raw-key (externally managed key) AES API.
/// </summary>
[TestClass]
public class AesRawKeyTest
{
    [TestMethod]
    public void EncryptWithKey_DecryptWithKey_ShouldRoundTrip()
    {
        var key = RandomNumberGenerator.GetBytes(32); // AES-256
        var data = Encoding.UTF8.GetBytes("EasilyNET raw-key AES round-trip");
        var cipher = AesCrypt.EncryptWithKey(data, key);
        var plain = AesCrypt.DecryptWithKey(cipher, key);
        CollectionAssert.AreEqual(data, plain);
    }

    [TestMethod]
    public void EncryptWithKey_ShouldUseRandomIv_ProducingDifferentCiphertexts()
    {
        var key = RandomNumberGenerator.GetBytes(16); // AES-128
        var data = Encoding.UTF8.GetBytes("same plaintext");
        var c1 = AesCrypt.EncryptWithKey(data, key);
        var c2 = AesCrypt.EncryptWithKey(data, key);
        // Random IV per call => ciphertexts differ, but both decrypt back to the same plaintext.
        Assert.IsFalse(c1.AsSpan().SequenceEqual(c2));
        CollectionAssert.AreEqual(data, AesCrypt.DecryptWithKey(c1, key));
        CollectionAssert.AreEqual(data, AesCrypt.DecryptWithKey(c2, key));
    }

    [TestMethod]
    public void DecryptWithKey_WrongKey_ShouldThrow()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var wrong = RandomNumberGenerator.GetBytes(32);
        var cipher = AesCrypt.EncryptWithKey(Encoding.UTF8.GetBytes("secret"), key);
        Assert.ThrowsExactly<CryptographicException>(() => AesCrypt.DecryptWithKey(cipher, wrong));
    }

    [TestMethod]
    public void EncryptWithKey_InvalidKeyLength_ShouldThrow()
    {
        var badKey = RandomNumberGenerator.GetBytes(20); // not 16/24/32
        Assert.ThrowsExactly<ArgumentException>(() => AesCrypt.EncryptWithKey(Encoding.UTF8.GetBytes("x"), badKey));
    }
}
