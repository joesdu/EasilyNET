using EasilyNET.Core.Enums;
using EasilyNET.Core.Misc;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.IdCard;

/// <summary>
/// 身份证校验
/// </summary>
/// <example>
/// <code>
///<![CDATA[
/// // 校验身份证号码是否合法.
/// "51132119xxxxxxxxxxxxxxx".ValidateIDCard();
/// ]]>
/// </code>
/// </example>
public static class IDCardCalculate
{
    /// <summary>
    /// 验证身份证号码
    /// </summary>
    /// <param name="no">身份证号码</param>
    /// <exception cref="ArgumentException">身份证号码不合法</exception>
    private static void ValidateIDCard(this string no)
    {
        if (no.CheckIDCard()) return;
        throw new ArgumentException($"身份证号不合法:{no}");
    }

    /// <summary>
    /// 根据身份证号码计算生日日期
    /// </summary>
    /// <param name="no">身份证号码</param>
    /// <param name="birthday">生日日期</param>
    /// <exception cref="ArgumentException"></exception>
    public static void CalculateBirthday(this string no, out DateTime birthday)
    {
        no.ValidateIDCard();
        birthday = no.Length switch
        {
            18 => $"{no.Substring(6, 4)}-{no.Substring(10, 2)}-{no.Substring(12, 2)}".ToDateTime(),
            15 => $"19{no.Substring(6, 2)}-{no.Substring(8, 2)}-{no.Substring(10, 2)}".ToDateTime(),
            _  => throw new("该身份证号无法正确计算出生日")
        };
    }

    /// <summary>
    /// 根据出生日期，计算精确的年龄
    /// </summary>
    /// <param name="birthday">生日(<see cref="DateTime"/>)</param>
    /// <returns>精确年龄</returns>
    public static int CalculateAge(DateTime birthday)
    {
        var now = DateTime.Now;
        var age = now.Year - birthday.Year;
        //再考虑月、天的因素
        if (now.Month < birthday.Month || now.Month != birthday.Month || now.Day >= birthday.Day) age--;
        return age;
    }

    /// <summary>
    /// 根据身份证号码计算出性别
    /// </summary>
    /// <param name="no">身份证号码</param>
    /// <returns><see cref="EGender"/> 性别</returns>
    public static EGender CalculateGender(this string no)
    {
        no.ValidateIDCard();
        //性别代码为偶数是女性奇数为男性
        return no.Length switch
        {
            18 => int.Parse(no.Substring(14, 3)) % 2 == 0 ? EGender.女 : EGender.男,
            15 => int.Parse(no.Substring(12, 3)) % 2 == 0 ? EGender.女 : EGender.男,
            _  => EGender.女
        };
    }

    /// <summary>
    /// 根据身份证号码计算生日日期
    /// </summary>
    /// <param name="no">身份证号</param>
    /// <param name="birthday">生日(<see cref="DateOnly"/>)</param>
    public static void CalculateBirthday(this string no, out DateOnly birthday)
    {
        no.ValidateIDCard();
        birthday = no.Length switch
        {
            18 => DateOnly.FromDateTime($"{no.Substring(6, 4)}-{no.Substring(10, 2)}-{no.Substring(12, 2)}".ToDateTime()),
            15 => DateOnly.FromDateTime($"19{no.Substring(6, 2)}-{no.Substring(8, 2)}-{no.Substring(10, 2)}".ToDateTime()),
            _  => throw new("该身份证号无法正确计算出生日")
        };
    }

    /// <summary>
    /// 根据出生日期，计算精确的年龄
    /// </summary>
    /// <param name="birthday">生日(<see cref="DateOnly"/>)</param>
    /// <returns>精确年龄</returns>
    public static int CalculateAge(DateOnly birthday)
    {
        var now = DateTime.Now;
        var age = now.Year - birthday.Year;
        //再考虑月、天的因素
        if (now.Month < birthday.Month || now.Month != birthday.Month || now.Day >= birthday.Day) age--;
        return age;
    }
}