using System.Text;
using EasilyNET.Security;
using Shouldly;

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
        Console.WriteLine(Convert.ToBase64String(signature));
        // 验证签名
        var verify = Sm2Crypt.Verify(pub_bytes, data_bytes, signature);
        verify.ShouldBeTrue();
    }

    /// <summary>
    /// Signature and Verification
    /// </summary>
    [TestMethod]
    public void SM2TestWithUserId()
    {
        // 公钥
        const string pub = "043D3AA4732B56DFCC643CF1B0ABAF75EF9EC16A756C18967090E8250E0A49915EEFDD5CBE16BB34CC93B20D3EFB4C842FFCE13887FE211DAE33DFD2AD025265D6";
        var pub_bytes = Convert.FromHexString(pub);
        // 私钥
        const string pri = "022A88CD5B5B3F56500615EF380C245CE81F32E980BB0CC708D01F9BD8558FEF";
        var pri_bytes = Convert.FromHexString(pri);
        // 测试数据
        const string data = "Microsoft";
        var data_bytes = Encoding.UTF8.GetBytes(data);
        const string user_id = "123456789";
        var userid_bytes = Encoding.UTF8.GetBytes(user_id);

        // 签名
        var signature = Sm2Crypt.Signature(pri_bytes, data_bytes, userid_bytes);
        Console.WriteLine(Convert.ToBase64String(signature));
        // 验证签名
        var verify = Sm2Crypt.Verify(pub_bytes, data_bytes, signature, userid_bytes);
        verify.ShouldBeTrue();
    }
}