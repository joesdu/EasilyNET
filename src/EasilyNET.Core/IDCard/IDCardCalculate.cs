using EasilyNET.Core.Enums;
using EasilyNET.Core.Misc;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.IdCard;

/// <summary>
///     <para xml:lang="en">ID card validation</para>
///     <para xml:lang="zh">身份证校验</para>
///     <example>
///         <code>
/// <![CDATA[
///  // 校验身份证号码是否合法.
///  "51132119xxxxxxxxxxxxxxx".ValidateIDCard();
///  ]]>
///  </code>
///     </example>
/// </summary>
public static class IDCardCalculate
{
    /// <summary>
    ///     <para xml:lang="en">Validate ID card number</para>
    ///     <para xml:lang="zh">验证身份证号码</para>
    /// </summary>
    /// <param name="no">
    ///     <para xml:lang="en">ID card number</para>
    ///     <para xml:lang="zh">身份证号码</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the ID card number is invalid</para>
    ///     <para xml:lang="zh">身份证号码不合法时抛出</para>
    /// </exception>
    public static void ValidateIDCard(this string no)
    {
        if (no.CheckIDCard())
            return;
        throw new ArgumentException($"身份证号不合法:{no}");
    }

    /// <summary>
    ///     <para xml:lang="en">Calculate birthdate from ID card number</para>
    ///     <para xml:lang="zh">根据身份证号码计算生日日期</para>
    /// </summary>
    /// <param name="no">
    ///     <para xml:lang="en">ID card number</para>
    ///     <para xml:lang="zh">身份证号码</para>
    /// </param>
    /// <param name="birthday">
    ///     <para xml:lang="en">Birthdate</para>
    ///     <para xml:lang="zh">生日日期</para>
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public static void CalculateBirthday(this string no, out DateTime birthday)
    {
        no.ValidateIDCard();
        birthday = no.Length switch
        {
            18 => ParseDateTime(no.AsSpan(6, 8)),
            15 => ParseDateTime($"19{no.AsSpan(6, 6)}"),
            _ => throw new ArgumentException("该身份证号无法正确计算出生日")
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Calculate gender from ID card number</para>
    ///     <para xml:lang="zh">根据身份证号码计算出性别</para>
    /// </summary>
    /// <param name="no">
    ///     <para xml:lang="en">ID card number</para>
    ///     <para xml:lang="zh">身份证号码</para>
    /// </param>
    public static EGender CalculateGender(this string no)
    {
        no.ValidateIDCard();
        // 性别代码为偶数是女性，奇数为男性
        return no.Length switch
        {
            18 => (no[16] - 48) % 2 == 0 ? EGender.女 : EGender.男,
            15 => (no[14] - 48) % 2 == 0 ? EGender.女 : EGender.男,
            _ => EGender.女
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Calculate exact age from birthdate</para>
    ///     <para xml:lang="zh">根据出生日期，计算精确的年龄</para>
    /// </summary>
    /// <param name="birthday">
    ///     <para xml:lang="en">Birthdate (<see cref="DateOnly" />)</para>
    ///     <para xml:lang="zh">生日(<see cref="DateOnly" />)</para>
    /// </param>
    public static int CalculateAge(DateOnly birthday)
    {
        var now = DateTime.Now;
        var age = now.Year - birthday.Year;
        // 再考虑月、天的因素
        if (now.Month < birthday.Month || (now.Month == birthday.Month && now.Day < birthday.Day))
            age--;
        return age;
    }

    /// <summary>
    ///     <para xml:lang="en">Calculate exact age from birthdate</para>
    ///     <para xml:lang="zh">根据出生日期，计算精确的年龄</para>
    /// </summary>
    /// <param name="birthday">
    ///     <para xml:lang="en">Birthdate (<see cref="DateTime" />)</para>
    ///     <para xml:lang="zh">生日(<see cref="DateTime" />)</para>
    /// </param>
    public static int CalculateAge(DateTime birthday) => CalculateAge(birthday.DateOnly);

    /// <summary>
    ///     <para xml:lang="en">Calculate birthdate from ID card number</para>
    ///     <para xml:lang="zh">根据身份证号码计算生日日期</para>
    /// </summary>
    /// <param name="no">
    ///     <para xml:lang="en">ID card number</para>
    ///     <para xml:lang="zh">身份证号</para>
    /// </param>
    /// <param name="birthday">
    ///     <para xml:lang="en">Birthdate (<see cref="DateOnly" />)</para>
    ///     <para xml:lang="zh">生日(<see cref="DateOnly" />)</para>
    /// </param>
    public static void CalculateBirthday(this string no, out DateOnly birthday)
    {
        no.CalculateBirthday(out DateTime date);
        birthday = date.DateOnly;
    }

    /// <summary>
    ///     <para xml:lang="en">Parse date string to <see cref="DateTime" /></para>
    ///     <para xml:lang="zh">解析日期字符串为 <see cref="DateTime" /></para>
    /// </summary>
    /// <param name="dateSpan">
    ///     <para xml:lang="en">Date string</para>
    ///     <para xml:lang="zh">日期字符串</para>
    /// </param>
    private static DateTime ParseDateTime(ReadOnlySpan<char> dateSpan)
    {
        var year = int.Parse(dateSpan[..4]);
        var month = int.Parse(dateSpan.Slice(4, 2));
        var day = int.Parse(dateSpan.Slice(6, 2));
        return new(year, month, day);
    }
}