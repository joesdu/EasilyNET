using System.ComponentModel;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Enums;

/// <summary>
///     <para xml:lang="en">Constellations</para>
///     <para xml:lang="zh">星座</para>
/// </summary>
public enum EConstellation
{
    /// <summary>
    ///     <para xml:lang="en">Capricorn: ♑ [12.22-1.19]</para>
    ///     <para xml:lang="zh">摩羯座: ♑ [12.22-1.19]</para>
    /// </summary>
    [Description("Capricorn")]
    摩羯座 = 0,

    /// <summary>
    ///     <para xml:lang="en">Aquarius: ♒ [1.20-2.18]</para>
    ///     <para xml:lang="zh">水瓶座: ♒ [1.20-2.18]</para>
    /// </summary>
    [Description("Aquarius")]
    水瓶座 = 1,

    /// <summary>
    ///     <para xml:lang="en">Pisces: ♓ [2.19-3.20]</para>
    ///     <para xml:lang="zh">双鱼座: ♓ [2.19-3.20]</para>
    /// </summary>
    [Description("Pisces")]
    双鱼座 = 2,

    /// <summary>
    ///     <para xml:lang="en">Aries: ♈ [3.21-4.19]</para>
    ///     <para xml:lang="zh">白羊座: ♈ [3.21-4.19]</para>
    /// </summary>
    [Description("Aries")]
    白羊座 = 3,

    /// <summary>
    ///     <para xml:lang="en">Taurus: ♉ [4.20-5.20]</para>
    ///     <para xml:lang="zh">金牛座: ♉ [4.20-5.20]</para>
    /// </summary>
    [Description("Taurus")]
    金牛座 = 4,

    /// <summary>
    ///     <para xml:lang="en">Gemini: ♊ [5.21-6.21]</para>
    ///     <para xml:lang="zh">双子座: ♊ [5.21-6.21]</para>
    /// </summary>
    [Description("Gemini")]
    双子座 = 5,

    /// <summary>
    ///     <para xml:lang="en">Cancer: ♋ [6.22-7.22]</para>
    ///     <para xml:lang="zh">巨蟹座: ♋ [6.22-7.22]</para>
    /// </summary>
    [Description("Cancer")]
    巨蟹座 = 6,

    /// <summary>
    ///     <para xml:lang="en">Leo: ♌ [7.23-8.22]</para>
    ///     <para xml:lang="zh">狮子座: ♌ [7.23-8.22]</para>
    /// </summary>
    [Description("Leo")]
    狮子座 = 7,

    /// <summary>
    ///     <para xml:lang="en">Virgo: ♍ [8.23-9.22]</para>
    ///     <para xml:lang="zh">处女座: ♍ [8.23-9.22]</para>
    /// </summary>
    [Description("Virgo")]
    处女座 = 8,

    /// <summary>
    ///     <para xml:lang="en">Libra: ♎ [9.23-10.23]</para>
    ///     <para xml:lang="zh">天秤座: ♎ [9.23-10.23]</para>
    /// </summary>
    [Description("Libra")]
    天秤座 = 9,

    /// <summary>
    ///     <para xml:lang="en">Scorpio: ♏ [10.24-11.22]</para>
    ///     <para xml:lang="zh">天蝎座: ♏ [10.24-11.22]</para>
    /// </summary>
    [Description("Scorpio")]
    天蝎座 = 10,

    /// <summary>
    ///     <para xml:lang="en">Sagittarius: ♐ [11.23-12.21]</para>
    ///     <para xml:lang="zh">射手座: ♐ [11.23-12.21]</para>
    /// </summary>
    [Description("Sagittarius")]
    射手座 = 11
}