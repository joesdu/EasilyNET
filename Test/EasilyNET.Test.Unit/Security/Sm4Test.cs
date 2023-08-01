using EasilyNET.Security;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// SM4测试
/// </summary>
[TestClass]
public class Sm4Test
{
    /// <summary>
    /// SM4ECB
    /// </summary>
    [TestMethod]
    public void Sm4EncryptECBTest()
    {
        const string data = """
                            {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                            """;
        var result = Sm4Crypt.EncryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, data);
        result.Should().Be("lhBbsI7A8sQS0ompMeOc7fFq/msSH4gPtmtIcjwneM+b7puu94OAztNbI+A8iSRYiEgErQQOrd2M9B8iAmrXJ21GNtwN2NN3Rc2xt8HDp4un7Orn+fXWpk7EEGjMH/El1gXnBXVdl1+U7+mw8L+T19RE4NgWcGsyeGck33R9iAha8oBMNN9xbPZZHnXGJE/3");
        result.Base64ToHex16().ToUpper().Should()
              .Be(
                  "96105BB08EC0F2C412D289A931E39CEDF16AFE6B121F880FB66B48723C2778CF9BEE9BAEF78380CED35B23E03C892458884804AD040EADDD8CF41F22026AD7276D4636DC0DD8D37745CDB1B7C1C3A78BA7ECEAE7F9F5D6A64EC41068CC1FF125D605E705755D975F94EFE9B0F0BF93D7D444E0D816706B32786724DF747D88085AF2804C34DF716CF6591E75C6244FF7");
    }

    /// <summary>
    /// SM4ECB
    /// </summary>
    [TestMethod]
    public void Sm4DecryptECBTest()
    {
        var data = """
                   lhBbsI7A8sQS0ompMeOc7fFq/msSH4gPtmtIcjwneM+b7puu94OAztNbI+A8iSRYiEgErQQOrd2M9B8iAmrXJ21GNtwN2NN3Rc2xt8HDp4un7Orn+fXWpk7EEGjMH/El1gXnBXVdl1+U7+mw8L+T19RE4NgWcGsyeGck33R9iAha8oBMNN9xbPZZHnXGJE/3
                   """;
        var result = Sm4Crypt.DecryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, data);
        result.Should().Be("""
                           {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                           """);
    }

    /// <summary>
    /// SM4ECB
    /// </summary>
    [TestMethod]
    public void Sm4EncryptECBTest2()
    {
        const string data = """
                            {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                            """;
        var result = Sm4Crypt.EncryptECB("701d1cc0cfbe7ee11824df718855c0c6".Substring(4, 16), false, data);
        result.Base64ToHex16().ToUpper().Should()
              .Be(
                  "DB52ACDAF28DF8B0190440E59AEB30112E44601AE66EA2845CFD3FB59BB36D064163CE992BFE317D6575AAB2DB64F4136FAC3160C26FC6563B57A0D8832DBF7C9C5D0184AFF1672CB28BC0EC424EFA84878872F346CF4F41020BD3D675BB797E108BE6DA04EB201FDF4633A37C91DEE16CBA75BF8F6E92A4EBF943D7B741CEB9418C39FD9CE10257218FCF33F885AE95");
    }

    /// <summary>
    /// SM4ECB
    /// </summary>
    [TestMethod]
    public void Sm4DecryptECBTest2()
    {
        const string data = """
                            DB52ACDAF28DF8B0190440E59AEB30112E44601AE66EA2845CFD3FB59BB36D064163CE992BFE317D6575AAB2DB64F4136FAC3160C26FC6563B57A0D8832DBF7C9C5D0184AFF1672CB28BC0EC424EFA84878872F346CF4F41020BD3D675BB797E108BE6DA04EB201FDF4633A37C91DEE16CBA75BF8F6E92A4EBF943D7B741CEB9418C39FD9CE10257218FCF33F885AE95
                            """;
        var result = Sm4Crypt.DecryptECB("701d1cc0cfbe7ee11824df718855c0c6".Substring(4, 16), false, data.Hex16ToBase64());
        result.Should().Be("""
                           {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                           """);
    }

    /// <summary>
    /// SM4CBC
    /// </summary>
    [TestMethod]
    public void Sm4EncryptCBCTest()
    {
        const string data = """
                            {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                            """;
        var result = Sm4Crypt.EncryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", data);
        result.ToUpper().Should()
              .Be(
                  "E5194A13A33ED904753D2A872BC417DD75085D721B97A1BBF1DDD58D2CF44F6B780E8A9175D7D3C6AA9ED011A59800EE12FC6C1397F8715FA24BE28491322DB00AFF5F1F7BD53D6B29654DEBD9F8F9620773E358EBC7FDCE3AA64E21D48EEBC7368274AF91B5EB3D1F033DC3463CDF8F21F8828EC16DB019AF0103BBF6C31F30A07E616138BF791D6F1E7DCFDD7EB4F4");
        result.Hex16ToBase64().ToUpper().Should()
              .Be("5RLKE6M+2QR1PSQHK8QX3XUIXXIBL6G78D3VJSZ0T2T4DOQRDDFTXQQE0BGLMADUEVXSE5F4CV+IS+KEKTITSAR/XX971T1RKWVN69N4+WIHC+NY68F9ZJQMTIHUJUVHNOJ0R5G16Z0FAZ3DRJZFJYH4GO7BBBAZRWEDU/BDHZCGFMFHOL95HW8EFC/DFRT0");
    }

    /// <summary>
    /// SM4CBC
    /// </summary>
    [TestMethod]
    public void Sm4DecryptCBCTest()
    {
        const string data = """
                            E5194A13A33ED904753D2A872BC417DD75085D721B97A1BBF1DDD58D2CF44F6B780E8A9175D7D3C6AA9ED011A59800EE12FC6C1397F8715FA24BE28491322DB00AFF5F1F7BD53D6B29654DEBD9F8F9620773E358EBC7FDCE3AA64E21D48EEBC7368274AF91B5EB3D1F033DC3463CDF8F21F8828EC16DB019AF0103BBF6C31F30A07E616138BF791D6F1E7DCFDD7EB4F4
                            """;
        var result = Sm4Crypt.DecryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", data);
        result.Should().Be("""
                           {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                           """);
    }

    /// <summary>
    /// SM4CBC
    /// </summary>
    [TestMethod]
    public void Sm4EncryptCBCTest2()
    {
        const string data = """
                            {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                            """;
        var result = Sm4Crypt.EncryptCBC("701d1cc0cfbe7ee11824df718855c0c6".Substring(4, 16), false, "701d1cc0cfbe7ee12824df718855c0c5".Substring(4, 16), data);
        result.ToUpper().Should()
              .Be(
                  "68929822CC5B0AA0358578A62D5AAF38262FFF44D64BABFB08AFED15F65882CC76BB7571171147ED62640945C85011151E2043413233648A2B62F5409516A7023D1089AAD3D20BB455C8AC68409F717BA4EBCFC490E12FEC6A9E23D8687DB08BC3C6784330FFEAD7F3CFDF0DB9E7D96472A5D461479FD6527E0F35A0DBE44C393D10B0A1FFFA2A03CC6E2587093440B6");
        result.Hex16ToBase64().ToUpper().Should()
              .Be("AJKYISXBCQA1HXIMLVQVOCYV/0TWS6V7CK/TFFZYGSX2U3VXFXFH7WJKCUXIUBEVHIBDQTIZZIORYVVALRANAJ0QIART0GU0VCISAECFCXUK68/EKOEV7GQEI9HOFBCLW8Z4QZD/6TFZZ98NUEFZZHKL1GFHN9ZSFG81ONVKTDK9ELCH//OQA8XUJYCJNEC2");
    }

    /// <summary>
    /// SM4CBC
    /// </summary>
    [TestMethod]
    public void Sm4DecryptCBCTest2()
    {
        const string data = """
                            68929822CC5B0AA0358578A62D5AAF38262FFF44D64BABFB08AFED15F65882CC76BB7571171147ED62640945C85011151E2043413233648A2B62F5409516A7023D1089AAD3D20BB455C8AC68409F717BA4EBCFC490E12FEC6A9E23D8687DB08BC3C6784330FFEAD7F3CFDF0DB9E7D96472A5D461479FD6527E0F35A0DBE44C393D10B0A1FFFA2A03CC6E2587093440B6
                            """;
        var result = Sm4Crypt.DecryptCBC("701d1cc0cfbe7ee11824df718855c0c6".Substring(4, 16), false, "701d1cc0cfbe7ee12824df718855c0c5".Substring(4, 16), data);
        result.Should().Be("""
                           {"dataObjName": "ODS_TEST", "fileds": [{"ID": "E9693FA22F384A7195944D4AF52AF52A", "AGE": 101, "NAME": "test-name", "DAY": "20230524"}]}
                           """);
    }
}