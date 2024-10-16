namespace EasilyNET.Core.IdCard;

/// <summary>
/// 身份证合理性验证
/// </summary>
public static class IDCardValidation
{
    private const string address = "11x22x35x44x53x12x23x36x45x54x13x31x37x46x61x14x32x41x50x62x15x33x42x51x63x21x34x43x52x64x65x71x81x82x91";
    private const string verifyCode = "10X98765432";
    private static readonly int[] Wi = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];

    /// <summary>
    /// 验证身份证合理性
    /// </summary>
    /// <param name="no">身份证号码</param>
    /// <returns>是否有效</returns>
    public static bool CheckIDCard(this string no) =>
        no.Length switch
        {
            18 => CheckIDCard18(no.AsSpan()),
            15 => CheckIDCard15(no.AsSpan()),
            _ => false
        };

    /// <summary>
    /// 18 位身份证号码验证
    /// </summary>
    private static bool CheckIDCard18(ReadOnlySpan<char> no)
    {
        if (!IsValidNumber(no[..17], 1_0000_0000_0000_0000L)) return false;
        if (!IsValidProvince(no[..2])) return false;
        if (!DateTime.TryParse($"{no.Slice(6, 4)}-{no.Slice(10, 2)}-{no.Slice(12, 2)}", out _)) return false; // 生日验证
        var sum = 0;
        for (var i = 0; i < 17; i++)
        {
            sum += Wi[i] * (no[i] - 48);
        }
        var y = sum % 11;
        return verifyCode[y] == no[17];
    }

    /// <summary>
    /// 15 位身份证号码验证
    /// </summary>
    private static bool CheckIDCard15(ReadOnlySpan<char> no)
    {
        if (!IsValidNumber(no, 1_0000_0000_0000_00L)) return false;
        return IsValidProvince(no[..2]) && DateTime.TryParse($"19{no.Slice(6, 2)}-{no.Slice(8, 2)}-{no.Slice(10, 2)}", out _); // 生日验证
    }

    /// <summary>
    /// 验证数字是否有效
    /// </summary>
    /// <param name="number">要验证的数字</param>
    /// <param name="minValue">最小值</param>
    /// <returns>是否有效</returns>
    private static bool IsValidNumber(ReadOnlySpan<char> number, long minValue) => long.TryParse(number, out var n) && n >= minValue;

    /// <summary>
    /// 验证省份代码是否有效
    /// </summary>
    /// <param name="province">省份代码</param>
    /// <returns>是否有效</returns>
    private static bool IsValidProvince(ReadOnlySpan<char> province) => address.Contains(province.ToString());
}