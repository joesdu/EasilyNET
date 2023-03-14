using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Enums;

/// <summary>
/// 中国民族枚举
/// </summary>
// ReSharper disable once UnusedType.Global
public enum ENation
{
    /*
     *代码

01 汉族      30 土族
02 蒙古族    31 达斡尔族
03 回族      32 仫佬族
04 藏族      33 羌族
05 维吾尔族   34 布朗族
06 苗族      35 撒拉族
07 彝族      36 毛南族
08 壮族      37 仡佬族
09 布依族    38 锡伯族
10 朝鲜族    39 阿昌族
11 满族      40 普米族
12 侗族      41 塔吉克族
13 瑶族      42 怒族
14 白族      43 乌孜别克族
15 土家族    44 俄罗斯族
16 哈尼族    45 鄂温克族
17 哈萨克族 4 6 德昂族
18 傣族      47 保安族
19 黎族      48 裕固族
20 傈僳族     49 京族
21 佤族      50 塔塔尔族
22 畲族      51 独龙族
23 高山族    52 鄂伦春族
24 拉祜族    53 赫哲族
25 水族      54 门巴族
26 东乡族     55 珞巴族
27 纳西族     56 基诺族
28 景颇族        57 其他
29 柯尔克孜族   58外国血统中国籍人士
     *
     */
    /// <summary>
    /// Han
    /// </summary>
    [Description("Han")]
    汉族 = 1,

    /// <summary>
    /// Manchu
    /// </summary>
    [Description("Manchu")]
    满族 = 2,

    /// <summary>
    /// Mongolian
    /// </summary>
    [Description("Mongolian")]
    蒙古族 = 3,

    /// <summary>
    /// Hui
    /// </summary>
    [Description("Hui")]
    回族 = 4,

    /// <summary>
    /// Tibetan
    /// </summary>
    [Description("Tibetan")]
    藏族 = 5,

    /// <summary>
    /// Uighur
    /// </summary>
    [Description("Uighur")]
    维吾尔族 = 6,

    /// <summary>
    /// Hmong
    /// </summary>
    [Description("Hmong")]
    苗族 = 7,

    /// <summary>
    /// Yi
    /// </summary>
    [Description("Yi")]
    彝族 = 8,

    /// <summary>
    /// Bouxcuengh
    /// </summary>
    [Description("Bouxcuengh")]
    壮族 = 9,

    /// <summary>
    /// Buxqyaix
    /// </summary>
    [Description("Buxqyaix")]
    布依族 = 10,

    /// <summary>
    /// Dong
    /// </summary>
    [Description("Dong")]
    侗族 = 11,

    /// <summary>
    /// Yao
    /// </summary>
    [Description("Yao")]
    瑶族 = 12,

    /// <summary>
    /// Bai
    /// </summary>
    [Description("Bai")]
    白族 = 13,

    /// <summary>
    /// Bizika
    /// </summary>
    [Description("Bizika")]
    土家族 = 14,

    /// <summary>
    /// HaNhi
    /// </summary>
    [Description("HaNhi")]
    哈尼族 = 15,

    /// <summary>
    /// Kazakh
    /// </summary>
    [Description("Kazakh")]
    哈萨克族 = 16,

    /// <summary>
    /// Dai
    /// </summary>
    [Description("Dai")]
    傣族 = 17,

    /// <summary>
    /// Li
    /// </summary>
    [Description("Li")]
    黎族 = 18,

    /// <summary>
    /// Lisu
    /// </summary>
    [Description("Lisu")]
    傈僳族 = 19,

    /// <summary>
    /// Va
    /// </summary>
    [Description("Va")]
    佤族 = 20,

    /// <summary>
    /// She
    /// </summary>
    [Description("She")]
    畲族 = 21,

    /// <summary>
    /// Gaoshan
    /// </summary>
    [Description("Gaoshan")]
    高山族 = 22,

    /// <summary>
    /// LadHull
    /// </summary>
    [Description("LadHull")]
    拉祜族 = 23,

    /// <summary>
    /// Sui
    /// </summary>
    [Description("Sui")]
    水族 = 24,

    /// <summary>
    /// Dongxiang
    /// </summary>
    [Description("Dongxiang")]
    东乡族 = 25,

    /// <summary>
    /// Nakhi
    /// </summary>
    [Description("Nakhi")]
    纳西族 = 26,

    /// <summary>
    /// Jingpo
    /// </summary>
    [Description("Jingpo")]
    景颇族 = 27,

    /// <summary>
    /// Kyrgyz
    /// </summary>
    [Description("Kyrgyz")]
    柯尔克孜族 = 28,

    /// <summary>
    /// Monguor
    /// </summary>
    [Description("Monguor")]
    土族 = 29,

    /// <summary>
    /// Daur
    /// </summary>
    [Description("Daur")]
    达斡尔族 = 30,

    /// <summary>
    /// Mulao
    /// </summary>
    [Description("Mulao")]
    仫佬族 = 31,

    /// <summary>
    /// Qiang
    /// </summary>
    [Description("Qiang")]
    羌族 = 32,

    /// <summary>
    /// Blang
    /// </summary>
    [Description("Blang")]
    布朗族 = 33,

    /// <summary>
    /// Salar
    /// </summary>
    [Description("Salar")]
    撒拉族 = 34,

    /// <summary>
    /// Maonan
    /// </summary>
    [Description("Maonan")]
    毛南族 = 35,

    /// <summary>
    /// Gelao
    /// </summary>
    [Description("Gelao")]
    仡佬族 = 36,

    /// <summary>
    /// Xibe
    /// </summary>
    [Description("Xibe")]
    锡伯族 = 37,

    /// <summary>
    /// Achang
    /// </summary>
    [Description("Achang")]
    阿昌族 = 38,

    /// <summary>
    /// Pumi
    /// </summary>
    [Description("Pumi")]
    普米族 = 39,

    /// <summary>
    /// Korean
    /// </summary>
    [Description("Korean")]
    朝鲜族 = 40,

    /// <summary>
    /// Tadzhik
    /// </summary>
    [Description("Tadzhik")]
    塔吉克族 = 41,

    /// <summary>
    /// Nu
    /// </summary>
    [Description("Nu")]
    怒族 = 42,

    /// <summary>
    /// Uzbek
    /// </summary>
    [Description("Uzbek")]
    乌孜别克族 = 43,

    /// <summary>
    /// Russian
    /// </summary>
    [Description("Russian")]
    俄罗斯族 = 44,

    /// <summary>
    /// Evenki
    /// </summary>
    [Description("Evenki")]
    鄂温克族 = 45,

    /// <summary>
    /// Deang
    /// </summary>
    [Description("Deang")]
    德昂族 = 46,

    /// <summary>
    /// Bonan
    /// </summary>
    [Description("Bonan")]
    保安族 = 47,

    /// <summary>
    /// Yughur
    /// </summary>
    [Description("Yughur")]
    裕固族 = 48,

    /// <summary>
    /// Kinh
    /// </summary>
    [Description("Kinh")]
    京族 = 49,

    /// <summary>
    /// Tatar
    /// </summary>
    [Description("Tatar")]
    塔塔尔族 = 50,

    /// <summary>
    /// Derung
    /// </summary>
    [Description("Derung")]
    独龙族 = 51,

    /// <summary>
    /// Oroqen
    /// </summary>
    [Description("Oroqen")]
    鄂伦春族 = 52,

    /// <summary>
    /// Nanai
    /// </summary>
    [Description("Nanai")]
    赫哲族 = 53,

    /// <summary>
    /// Monpa
    /// </summary>
    [Description("Monpa")]
    门巴族 = 54,

    /// <summary>
    /// Lhoba
    /// </summary>
    [Description("Lhoba")]
    珞巴族 = 55,

    /// <summary>
    /// Jino
    /// </summary>
    [Description("")]
    基诺族 = 56,

    /// <summary>
    /// Chuanqing
    /// </summary>
    [Description("Chuanqing")]
    穿青人 = 57,

    /// <summary>
    /// Rat:🐁
    /// </summary>
    [Description("")]
    外国血统中国籍人士 = 98,

    /// <summary>
    /// Rat:🐁
    /// </summary>
    [Description("")]
    其他 = 99
}