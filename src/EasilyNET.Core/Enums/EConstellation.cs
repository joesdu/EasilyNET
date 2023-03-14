using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Enums;

/// <summary>
/// 十二星座
/// </summary>
// ReSharper disable once UnusedType.Global
public enum EConstellation
{
    /// <summary>
    /// Capricorn: ♑ [12.22-1.19]
    /// </summary>
    [Description("Capricorn")]
    摩羯座 = 0,

    /// <summary>
    /// Aquarius: ♒ [1.20-2.18]
    /// </summary>
    [Description("Aquarius")]
    水瓶座 = 1,

    /// <summary>
    /// Pisces: ♓ [2.19-3.20]
    /// </summary>
    [Description("Pisces")]
    双鱼座 = 2,

    /// <summary>
    /// Aries: ♈ [3.21-4.19]
    /// </summary>
    [Description("Aries")]
    白羊座 = 3,

    /// <summary>
    /// Taurus: ♉ [4.20-5.20]
    /// </summary>
    [Description("Taurus")]
    金牛座 = 4,

    /// <summary>
    /// Gemini: ♊ [5.21-6.21]
    /// </summary>
    [Description("Gemini")]
    双子座 = 5,

    /// <summary>
    /// Cancer: ♋ [6.22-7.22]
    /// </summary>
    [Description("Cancer")]
    巨蟹座 = 6,

    /// <summary>
    /// Leo: ♌ [7.23-8.22]
    /// </summary>
    [Description("Leo")]
    狮子座 = 7,

    /// <summary>
    /// Virgo: ♍ [8.23-9.22]
    /// </summary>
    [Description("Virgo")]
    处女座 = 8,

    /// <summary>
    /// Libra: ♎ [9.23-10.23]
    /// </summary>
    [Description("Libra")]
    天秤座 = 9,

    /// <summary>
    /// Scorpio: ♏ [10.24-11.22]
    /// </summary>
    [Description("Scorpio")]
    天蝎座 = 10,

    /// <summary>
    /// Sagittarius: ♐ [11.23-12.21]
    /// </summary>
    [Description("Sagittarius")]
    射手座 = 11
}