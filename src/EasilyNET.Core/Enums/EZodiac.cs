using System.ComponentModel;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Enums;

/// <summary>
///     <para xml:lang="en">Chinese Zodiac</para>
///     <para xml:lang="zh">十二生肖</para>
/// </summary>
public enum EZodiac
{
    /// <summary>
    ///     <para xml:lang="en">Rat: 🐁</para>
    ///     <para xml:lang="zh">鼠: 🐁</para>
    /// </summary>
    [Description("Rat")]
    鼠 = 0,

    /// <summary>
    ///     <para xml:lang="en">Ox: 🐂</para>
    ///     <para xml:lang="zh">牛: 🐂</para>
    /// </summary>
    [Description("Ox")]
    牛 = 1,

    /// <summary>
    ///     <para xml:lang="en">Tiger: 🐅</para>
    ///     <para xml:lang="zh">虎: 🐅</para>
    /// </summary>
    [Description("Tiger")]
    虎 = 2,

    /// <summary>
    ///     <para xml:lang="en">Rabbit: 🐇</para>
    ///     <para xml:lang="zh">兔: 🐇</para>
    /// </summary>
    [Description("Rabbit")]
    兔 = 3,

    /// <summary>
    ///     <para xml:lang="en">Dragon: 🐉</para>
    ///     <para xml:lang="zh">龙: 🐉</para>
    /// </summary>
    [Description("Dragon")]
    龙 = 4,

    /// <summary>
    ///     <para xml:lang="en">Snake: 🐍</para>
    ///     <para xml:lang="zh">蛇: 🐍</para>
    /// </summary>
    [Description("Snake")]
    蛇 = 5,

    /// <summary>
    ///     <para xml:lang="en">Horse: 🐎</para>
    ///     <para xml:lang="zh">马: 🐎</para>
    /// </summary>
    [Description("Horse")]
    马 = 6,

    /// <summary>
    ///     <para xml:lang="en">Sheep: 🐏</para>
    ///     <para xml:lang="zh">羊: 🐏</para>
    /// </summary>
    [Description("Sheep")]
    羊 = 7,

    /// <summary>
    ///     <para xml:lang="en">Monkey: 🐒</para>
    ///     <para xml:lang="zh">猴: 🐒</para>
    /// </summary>
    [Description("Monkey")]
    猴 = 8,

    /// <summary>
    ///     <para xml:lang="en">Rooster: 🐓</para>
    ///     <para xml:lang="zh">鸡: 🐓</para>
    /// </summary>
    [Description("Rooster")]
    鸡 = 9,

    /// <summary>
    ///     <para xml:lang="en">Dog: 🐕</para>
    ///     <para xml:lang="zh">狗: 🐕</para>
    /// </summary>
    [Description("Dog")]
    狗 = 10,

    /// <summary>
    ///     <para xml:lang="en">Pig: 🐖</para>
    ///     <para xml:lang="zh">猪: 🐖</para>
    /// </summary>
    [Description("Pig")]
    猪 = 11
}