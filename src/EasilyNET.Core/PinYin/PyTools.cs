/*
 * 该类实现了取汉字文本首字母、文本对应拼音、以及
 * 获取和拼音对应的汉字列表等方法。由于汉字字库大，且多音字较多，因此本组中实现的
 * 拼音转换不一定和词语中的字的正确读音完全吻合。但绝大部分是正确的。
 *
 * 设计思路：
 * 将汉字按拼音分组后建立一个字符串数组（见PyCode.codes），然后使用程序
 * 将PyCode.codes中每一个汉字通过其编码值使用散列函数：
 *
 *     f(x) = x % PyCode.codes.Length
 *     g(f(x)) = pos(x)
 *
 * 其中, pos(x)为字符x所属字符串所在的PyCode.codes的数组下标, 然后散列到同PyCode.codes长度相同长度的一个散列表中PyHash.hashes。
 * 当检索一个汉字的拼音时，首先从PyHash.hashes中获取和对应的PyCode.codes中数组下标，然后从对应字符串查找，
 * 当到要查找的字符时，字符串的前6个字符即包含了该字的拼音。
 *
 * 此种方法的好处一是节约了存储空间，二是兼顾了查询效率。
 */

/*
 * =================================================================
 * v1.2.x的变化
 * =================================================================
 * 1.增加重构单字符拼音的获取,未找到拼音时返回特定字符串
 * 2.加入标点符号,控制符,10进制数字,空格,小写字母,大写字母,特殊符号,分隔符的兼容
 *
 * =================================================================
 * v0.2.x的变化
 * =================================================================
 * 1、增加对不同编码格式文本的支持,同时增加编码转换方法PyTools.ConvertEncoding
 * 2、重构单字符拼音的获取，未找到拼音时返回字符本身.
 */

using System.Text;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.PinYin;

/// <summary>
/// Public PinYin API
/// </summary>
public static class PyTools
{
    /// <summary>
    /// 取中文文本的拼音首字母
    /// </summary>
    /// <param name="text">编码为UTF8的文本</param>
    /// <returns>返回中文对应的拼音首字母</returns>
    public static string GetInitials(string text)
    {
        text = text.Trim();
        var sb = new StringBuilder();
        foreach (var py in text.Select(GetPinYin).Where(py => !string.IsNullOrWhiteSpace(py)))
        {
            _ = sb.Append(py[0].ToString().ToUpper());
        }
        return sb.ToString().ToUpper();
    }

    /// <summary>
    /// 取中文文本的拼音首字母
    /// </summary>
    /// <param name="text">编码为UTF8的文本</param>
    /// <param name="defaultStr">转换失败时返回的预设字符</param>
    /// <returns>返回中文对应的拼音首字母</returns>
    public static string GetInitials(string text, string defaultStr)
    {
        text = text.Trim();
        var sb = new StringBuilder();
        foreach (var py in text.Select(t => GetPinYin(t, defaultStr)))
        {
            _ = string.IsNullOrWhiteSpace(py) ? sb.Append(defaultStr) : sb.Append(py[0].ToString().ToUpper());
        }
        return sb.ToString().ToUpper();
    }

    /// <summary>
    /// 取中文文本的拼音首字母
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="encoding">源文本的编码</param>
    /// <returns>返回Encoding编码类型中文对应的拼音首字母</returns>
    public static string GetInitials(string text, Encoding encoding) => ConvertEncoding(GetInitials(ConvertEncoding(text, encoding, Encoding.UTF8)), Encoding.UTF8, encoding);

    /// <summary>
    /// 取中文文本的拼音首字母
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="defaultStr">转换失败后返回的预设字符</param>
    /// <param name="encoding">源文本的编码</param>
    /// <returns>返回Encoding编码类型中文对应的拼音首字母</returns>
    public static string GetInitials(string text, string defaultStr, Encoding encoding) => ConvertEncoding(GetInitials(ConvertEncoding(text, encoding, Encoding.UTF8), defaultStr), Encoding.UTF8, encoding);

    /// <summary>
    /// 取中文文本的拼音
    /// </summary>
    /// <param name="text">编码为UTF8的文本</param>
    /// <returns>返回中文文本的拼音</returns>
    public static string GetPinYin(string text)
    {
        var sb = new StringBuilder();
        foreach (var py in text.Select(GetPinYin).Where(py => !string.IsNullOrWhiteSpace(py)))
        {
            _ = sb.Append(py);
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// 取中文文本的拼音
    /// </summary>
    /// <param name="text">编码为UTF8的文本</param>
    /// <param name="defaultStr">当获取拼音失败时返回预设字符</param>
    /// <returns>返回中文文本的拼音</returns>
    public static string GetPinYin(string text, string defaultStr)
    {
        var sb = new StringBuilder();
        foreach (var py in text.Select(t => GetPinYin(t, defaultStr)))
        {
            _ = string.IsNullOrWhiteSpace(py) ? sb.Append(defaultStr) : sb.Append(py);
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// 返回单个字符的汉字拼音
    /// </summary>
    /// <param name="ch">编码为UTF8的中文字符</param>
    /// <returns>ch对应的拼音</returns>
    public static string GetPinYin(char ch)
    {
        //是否是标点符号,控制符,10进制数字,空格,小写字母,大写字母,特殊符号,分隔符
        if (char.IsPunctuation(ch) | char.IsControl(ch) | char.IsDigit(ch) | char.IsWhiteSpace(ch) | char.IsLower(ch) | char.IsUpper(ch) | char.IsSymbol(ch) | char.IsSeparator(ch)) return ch.ToString();
        var hash = GetHashIndex(ch);
        foreach (var index in PyHash.Hashes[hash])
        {
            var pos = PyCode.Codes[index].IndexOf(ch, 7);
            if (pos != -1) return $"{PyCode.Codes[index][..6].Trim()} ";
        }
        return ch.ToString();
    }

    /// <summary>
    /// 返回单个字符的汉字拼音
    /// </summary>
    /// <param name="ch">编码为UTF8的中文字符</param>
    /// <param name="defaultStr">转换失败后返回字符串</param>
    /// <returns>ch对应的拼音</returns>
    public static string GetPinYin(char ch, string defaultStr)
    {
        //是否是标点符号,控制符,10进制数字,空格,小写字母,大写字母,特殊符号,分隔符
        if (char.IsPunctuation(ch) | char.IsControl(ch) | char.IsDigit(ch) | char.IsWhiteSpace(ch) | char.IsLower(ch) | char.IsUpper(ch) | char.IsSymbol(ch) | char.IsSeparator(ch)) return ch.ToString();
        var hash = GetHashIndex(ch);
        foreach (var index in PyHash.Hashes[hash])
        {
            var pos = PyCode.Codes[index].IndexOf(ch, 7);
            if (pos != -1) return $"{PyCode.Codes[index][..6].Trim()} ";
        }
        return defaultStr;
    }

    /// <summary>
    /// 取中文文本的拼音
    /// </summary>
    /// <param name="text">编码为UTF8的文本</param>
    /// <param name="encoding">源文本的编码</param>
    /// <returns>返回Encoding编码类型的中文文本的拼音</returns>
    public static string GetPinYin(string text, Encoding encoding) => ConvertEncoding(GetPinYin(ConvertEncoding(text.Trim(), encoding, Encoding.UTF8)), Encoding.UTF8, encoding);

    /// <summary>
    /// 取中文文本的拼音
    /// </summary>
    /// <param name="text">编码为UTF8的文本</param>
    /// <param name="defaultStr">转换失败后返回的字符</param>
    /// <param name="encoding">源文本的编码</param>
    /// <returns>返回Encoding编码类型的中文文本的拼音</returns>
    public static string GetPinYin(string text, string defaultStr, Encoding encoding) => ConvertEncoding(GetPinYin(ConvertEncoding(text.Trim(), encoding, Encoding.UTF8), defaultStr), Encoding.UTF8, encoding);

    /// <summary>
    /// 返回单个字符的汉字拼音
    /// </summary>
    /// <param name="ch">编码为Encoding的中文字符</param>
    /// <param name="encoding">源字符编码</param>
    /// <returns>编码为Encoding的ch对应的拼音</returns>
    public static string GetPinYin(char ch, Encoding encoding)
    {
        ch = ConvertEncoding(ch.ToString(), encoding, Encoding.UTF8)[0];
        return ConvertEncoding(GetPinYin(ch), Encoding.UTF8, encoding);
    }

    /// <summary>
    /// 返回单个字符的汉字拼音
    /// </summary>
    /// <param name="defaultStr">当转换失败后返回的字符</param>
    /// <param name="ch">编码为Encoding的中文字符</param>
    /// <param name="encoding">源字符编码</param>
    /// <returns>编码为Encoding的ch对应的拼音</returns>
    public static string GetPinYin(char ch, string defaultStr, Encoding encoding)
    {
        ch = ConvertEncoding(ch.ToString(), encoding, Encoding.UTF8)[0];
        return ConvertEncoding(GetPinYin(ch, defaultStr), Encoding.UTF8, encoding);
    }

    /// <summary>
    /// 取和拼音相同的汉字列表
    /// </summary>
    /// <param name="pinyin">编码为UTF8的拼音</param>
    /// <returns>取拼音相同的汉字列表，如拼音“ai”将会返回“唉爱……”等</returns>
    public static string GetChineseText(string pinyin)
    {
        var key = pinyin.Trim().ToLower();
        foreach (var str in PyCode.Codes)
        {
            if (str.StartsWith($"{key} ") || str.StartsWith($"{key}:"))
                return str[7..];
        }
        return string.Empty;
    }

    /// <summary>
    /// 取和拼音相同的汉字列表，编码同参数encoding
    /// </summary>
    /// <param name="pinyin">编码为encoding的拼音</param>
    /// <param name="encoding">编码</param>
    /// <returns>返回编码为encoding的拼音为pinyin的汉字列表，如拼音“ai”将会返回“唉爱……”等</returns>
    public static string GetChineseText(string pinyin, Encoding encoding) => ConvertEncoding(GetChineseText(ConvertEncoding(pinyin, encoding, Encoding.UTF8)), Encoding.UTF8, encoding);

    /// <summary>
    /// 转换编码
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="srcEncoding">源编码</param>
    /// <param name="dstEncoding">目标编码</param>
    /// <returns>目标编码文本</returns>
    public static string ConvertEncoding(string text, Encoding srcEncoding, Encoding dstEncoding) => dstEncoding.GetString(Encoding.Convert(srcEncoding, dstEncoding, srcEncoding.GetBytes(text)));

    /// <summary>
    /// 取文本索引值
    /// </summary>
    /// <param name="ch">字符</param>
    /// <returns>文本索引值</returns>
    private static short GetHashIndex(char ch) => (short)((uint)ch % PyCode.Codes.Length);
}