using EasilyNET.Core.Language;

namespace EasilyNET.Core.IdCard;

/// <summary>
/// 身份证合理性验证
/// </summary>
public static class IDCardValidation
{
    private const string address = "11x22x35x44x53x12x23x36x45x54x13x31x37x46x61x14x32x41x50x62x15x33x42x51x63x21x34x43x52x64x65x71x81x82x91";
    private static readonly string[] verifyCode = { "1", "0", "X", "9", "8", "7", "6", "5", "4", "3", "2" };
    private static readonly int[] Wi = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };

    /// <summary>
    /// 验证身份证合理性
    /// </summary>
    /// <param name="no">身份证号码</param>
    /// <returns></returns>
    public static bool CheckIDCard(this string no) =>
        no.Length switch
        {
            18 => CheckIDCard18(no.ToUpper()),
            15 => CheckIDCard15(no.ToUpper()),
            _  => false
        };

    /// <summary>
    /// <see langword="18" /> 位身份证号码验证
    /// </summary>
    private static bool CheckIDCard18(string no)
    {
        if (long.TryParse(no.Remove(17), out var n) == false || n < Math.Pow(10, 16) || long.TryParse(no.Replace('x', '0').Replace('X', '0'), out _) == false) return false; //数字验证
        if (!address.Contains(no.Remove(2))) return false;                                                                                                                   //省份验证  
        var birth = no.Substring(6, 8).Insert(6, "-").Insert(4, "-");
        if (!DateTime.TryParse(birth, out _)) return false; //生日验证
        var Ai = no.Remove(17).ToCharArray();
        var sum = 0;
        foreach (var i in ..16)
        {
            sum += Wi[i] * int.Parse(Ai[i].ToString());
        }
        _ = Math.DivRem(sum, 11, out var y);
        return verifyCode[y] == no[17..];
    }

    /// <summary>
    /// <see langword="15" /> 位身份证号码验证
    /// </summary>
    private static bool CheckIDCard15(string no)
    {
        if (long.TryParse(no, out var n) == false || n < Math.Pow(10, 14)) return false; //数字验证
        if (!address.Contains(no.Remove(2))) return false;                               //省份验证  
        var birth = no.Substring(6, 6).Insert(4, "-").Insert(2, "-");
        return DateTime.TryParse(birth, out _);
    }
}