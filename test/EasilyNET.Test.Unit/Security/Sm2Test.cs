using EasilyNET.Core.Misc;
using EasilyNET.Security;
using FluentAssertions;
using System.Text;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// SM2
/// </summary>
[TestClass]
public class Sm2Test
{
    /// <summary>
    /// Signature and Verification
    /// </summary>
    [TestMethod]
    public void SM2Test1()
    {
        // 公钥
        const string pub = "BPVJQwD6ZOmtvktOF4WZOGskbkWug0KkD512i6mcqorPTE41J1ZlTWrkfBLvdgPlBcae3jHkBKapZfkPKlJJSCc=";
        var pub_bytes = Convert.FromBase64String(pub);
        // 私钥
        const string pri = "AOekUYQbqjPOexkPg77bTq53RXWjuqOUF5uMDuAggDV5";
        var pri_bytes = Convert.FromBase64String(pri);
        // 测试数据
        const string data = "Microsoft";
        var data_bytes = Encoding.UTF8.GetBytes(data);

        // 签名
        var signature = Sm2Crypt.Signature(pri_bytes, data_bytes);
        Console.WriteLine(signature.ToBase64());
        // 验证签名
        var verify = Sm2Crypt.Verify(pub_bytes, data_bytes, signature);
        verify.Should().BeTrue();
    }
}