using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Numerics;
using EasilyNET.Core.Essentials;
using EasilyNET.Core.Misc;
using EasilyNET.Core.Numerics;
using EasilyNET.Security;
using EasilyNET.WebCore.JsonConverters;

namespace EasilyNET.Test.Unit.Regression;

/// <summary>
/// 针对一批已修复的正确性缺陷的回归测试，确保不再退化。
/// </summary>
[TestClass]
public class Tier1CorrectnessTests
{
    #region NumberExtensions.AreAlmostEqual (previously truncated magnitudes via (sbyte) cast)

    [TestMethod]
    public void AreAlmostEqual_CloseSmallValues_ShouldBeTrue()
    {
        Assert.IsTrue(0.123456.AreAlmostEqual(0.1234561));
        Assert.IsTrue(0.0.AreAlmostEqual(0.0));
        // Large magnitude: relative tolerance must scale, not truncate to 0 via (sbyte).
        Assert.IsTrue(1_000_000.0.AreAlmostEqual(1_000_000.0 + 0.0001));
        Assert.IsTrue(1_000_000.0f.AreAlmostEqual(1_000_000.0f));
        Assert.IsTrue(1234.5678m.AreAlmostEqual(1234.5678m));
    }

    [TestMethod]
    public void AreAlmostEqual_DistinctValues_ShouldBeFalse()
    {
        Assert.IsFalse(1.0.AreAlmostEqual(2.0));
        Assert.IsFalse(0.0.AreAlmostEqual(1.0));
        Assert.IsFalse(100.0.AreAlmostEqual(100.5));
    }

    #endregion

    #region StringExtensions.IsPhoneNumber (previously a tautology + IndexOutOfRange)

    [TestMethod]
    public void IsPhoneNumber_ValidNumbers_ShouldBeTrue()
    {
        Assert.IsTrue("13800138000".IsPhoneNumber());
        Assert.IsTrue("19912345678".IsPhoneNumber());
    }

    [TestMethod]
    public void IsPhoneNumber_InvalidNumbers_ShouldBeFalse()
    {
        Assert.IsFalse("12800138000".IsPhoneNumber());  // second digit must be 3-9
        Assert.IsFalse("1380013800".IsPhoneNumber());   // 10 digits
        Assert.IsFalse("138001380000".IsPhoneNumber()); // 12 digits
        Assert.IsFalse("a3800138000".IsPhoneNumber());  // non-digit
        Assert.IsFalse("1".IsPhoneNumber());            // must not throw IndexOutOfRange
        Assert.IsFalse("".IsPhoneNumber());
    }

    #endregion

    #region StringExtensions.Validate ReDoS protection (match timeout)

    [TestMethod]
    public void Validate_CatastrophicBacktracking_ShouldTimeout()
    {
        // "(a+)+$" against many 'a's followed by a non-matching char triggers catastrophic backtracking.
        var input = new string('a', 40) + "!";
        Assert.ThrowsExactly<RegexMatchTimeoutException>(() => input.Validate("^(a+)+$", TimeSpan.FromMilliseconds(100)));
    }

    [TestMethod]
    public void Validate_NormalPattern_ShouldStillMatch()
    {
        Assert.IsTrue("12345".Validate(@"^\d+$"));
        Assert.IsFalse("12a45".Validate(@"^\d+$"));
    }

    #endregion

    #region Ulid.TryParse (previously accepted garbage Base32 input)

    [TestMethod]
    public void Ulid_TryParse_InvalidSymbols_ShouldReturnFalse()
    {
        // 26 chars, correct length but all invalid Base32 symbols.
        Assert.IsFalse(Ulid.TryParse(new string('!', 26), out _));
        // Crockford Base32 excludes I, L, O, U.
        Assert.IsFalse(Ulid.TryParse("IIIIIIIIIIIIIIIIIIIIIIIIII", out _));
    }

    [TestMethod]
    public void Ulid_TryParse_ValidValue_ShouldParse()
    {
        // Validates that the added Base32 symbol validation does not reject legitimate input.
        var text = Ulid.NewUlid().ToString();
        Assert.IsTrue(Ulid.TryParse(text, out var parsed));
        Assert.AreEqual(text, parsed.ToString());
    }

    #endregion

    #region PooledMemoryStream (previously exposed stale pooled data)

    [TestMethod]
    public void PooledMemoryStream_SetLengthGrow_ShouldZeroNewRegion()
    {
        using var stream = new PooledMemoryStream();
        stream.Write([1, 2, 3, 4, 5], 0, 5);
        stream.SetLength(64); // grow well past existing content
        stream.Position = 0;
        var buffer = new byte[64];
        var read = stream.Read(buffer, 0, 64);
        Assert.AreEqual(64, read);
        for (var i = 5; i < 64; i++)
        {
            Assert.AreEqual((byte)0, buffer[i], $"byte {i} must be zeroed, not stale pooled data");
        }
    }

    [TestMethod]
    public void PooledMemoryStream_WriteAfterSeekPastEnd_ShouldZeroGap()
    {
        using var stream = new PooledMemoryStream();
        stream.Write([1, 2, 3], 0, 3);
        stream.Seek(32, SeekOrigin.Begin); // leave a gap [3, 32)
        stream.Write([9, 9], 0, 2);
        stream.Position = 0;
        var buffer = new byte[34];
        _ = stream.Read(buffer, 0, 34);
        for (var i = 3; i < 32; i++)
        {
            Assert.AreEqual((byte)0, buffer[i], $"gap byte {i} must be zeroed");
        }
        Assert.AreEqual((byte)9, buffer[32]);
        Assert.AreEqual((byte)9, buffer[33]);
    }

    #endregion

    #region DecimalJsonConverter / DateOnlyJsonConverter (previously culture-dependent)

    [TestMethod]
    public void DecimalJsonConverter_ShouldWriteInvariantNumber_RegardlessOfCulture()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DecimalJsonConverter());
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new("de-DE"); // uses ',' as decimal separator
            var json = JsonSerializer.Serialize((decimal?)1.5m, options);
            Assert.AreEqual("1.5", json); // a real JSON number, invariant
            var back = JsonSerializer.Deserialize<decimal?>(json, options);
            Assert.AreEqual(1.5m, back);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void DateOnlyJsonConverter_ShouldRoundTripInvariant_RegardlessOfCulture()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateOnlyJsonConverter());
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new("ar-SA"); // non-Gregorian default calendar
            var value = (DateOnly?)new DateOnly(2024, 1, 2);
            var json = JsonSerializer.Serialize(value, options);
            Assert.AreEqual("\"2024-01-02\"", json);
            var back = JsonSerializer.Deserialize<DateOnly?>(json, options);
            Assert.AreEqual(value, back);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    #endregion

    #region SM4 PKCS7 padding (previously unvalidated -> padding oracle / crash)

    [TestMethod]
    public void Sm4_DecryptTamperedCiphertext_ShouldThrowCryptographicException()
    {
        const string key = "701d1cc0cfbe7ee11824df718855c0c6";
        var encrypted = Sm4Crypt.EncryptToBase64(key, true, "Microsoft");
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF; // corrupt the last ciphertext byte -> last block padding becomes invalid
        var tampered = Convert.ToBase64String(bytes);
        Assert.ThrowsExactly<CryptographicException>(() => Sm4Crypt.DecryptFromBase64(key, true, tampered));
    }

    #endregion

    #region BigNumber negative decimal parsing (previously dropped the sign on the fraction)

    [TestMethod]
    public void BigNumber_ParseNegativeDecimal_ShouldKeepSign()
    {
        var parsed = new BigNumber("-1,5");
        var expected = BigNumber.FromBigInteger(new BigInteger(-3), new BigInteger(2)); // -3/2 = -1.5
        Assert.AreEqual(expected, parsed);
    }

    #endregion
}
