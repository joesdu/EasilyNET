using System.Text;
using EasilyNET.Security;
using FluentAssertions;

// ReSharper disable StringLiteralTypo

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// RC4
/// </summary>
[TestClass]
public class Rc4Test
{
    /// <summary>
    /// RC4
    /// </summary>
    [TestMethod]
    public void Rc4()
    {
        const string data = "Microsoft";
        var key = "123456"u8.ToArray();
        var byte_data = Encoding.UTF8.GetBytes(data);
        var secret = Rc4Crypt.Encrypt(byte_data, key);
        var base64 = Convert.ToBase64String(secret);
        base64.Should().Be("TZEdFUtAevoL");
        var data_result = Rc4Crypt.Decrypt(secret, key);
        var result = Encoding.UTF8.GetString(data_result);
        result.Should().Be(data);
    }
}