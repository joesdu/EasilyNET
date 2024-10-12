using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EasilyNET.Core.Enums;

#pragma warning disable IDE0079

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// 字符串String扩展
/// </summary>
public static partial class StringExtension
{
    /// <summary>
    /// 移除字符串中所有空白符
    /// </summary>
    /// <param name="value">字符串</param>
    /// <returns></returns>
    public static string RemoveWhiteSpace(this string value) => RemoveWhiteSpaceRegex().Replace(value, string.Empty);

    [GeneratedRegex(@"\s")]
    private static partial Regex RemoveWhiteSpaceRegex();

    /// <summary>
    /// 以特定字符间隔的字符串转化为字符串集合
    /// </summary>
    /// <param name="value">需要处理的字符串</param>
    /// <param name="separator">分隔此实例中子字符串</param>
    /// <returns>转化后的字符串集合，如果传入数组为null则返回空集合</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static StringCollection ToStringCollection(this string value, string separator)
    {
        var col = new StringCollection();
        if (string.IsNullOrEmpty(separator) || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(value.Trim()))
        {
            return col;
        }
        var index = 0;
        var pos = 0;
        var len = separator.Length;
        while (pos >= 0)
        {
            pos = value.IndexOf(separator, index, StringComparison.CurrentCultureIgnoreCase);
            col.Add(pos >= 0 ? value[index..pos] : value[index..]);
            index = pos + len;
        }
        return col;
    }

    /// <summary>
    /// 将字符串中的单词首字母大写或者小写
    /// </summary>
    /// <param name="value">单词</param>
    /// <param name="lower">是否小写? 默认:true</param>
    /// <returns></returns>
    public static string ToTitleUpperCase(this string value, bool lower = true)
    {
        var regex = ToTitleUpperCaseRegex();
        return regex.Replace(value,
            delegate (Match m)
            {
                var str = m.ToString();
                if (!char.IsLower(str[0])) return str;
                var header = lower ? char.ToLower(str[0], CultureInfo.CurrentCulture) : char.ToUpper(str[0], CultureInfo.CurrentCulture);
                return $"{header}{str[1..]}";
            });
    }

    [GeneratedRegex(@"\w+")]
    private static partial Regex ToTitleUpperCaseRegex();

    /// <summary>
    /// 字符串插入指定分隔符
    /// </summary>
    /// <param name="text">字符串</param>
    /// <param name="spacingString">分隔符</param>
    /// <param name="spacingIndex">隔多少个字符插入分隔符</param>
    /// <returns></returns>
    public static string Spacing(this string text, string spacingString, int spacingIndex)
    {
        var sb = new StringBuilder(text);
        for (var i = spacingIndex; i <= sb.Length; i += spacingIndex + 1)
        {
            if (i >= sb.Length) break;
            _ = sb.Insert(i, spacingString);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 检查一个字符串是否是纯数字构成的,一般用于查询字符串参数的有效性验证
    /// </summary>
    /// <param name="value">需验证的字符串</param>
    /// <returns>是否合法的bool值</returns>
    public static bool IsNumber(this string value) => value.Validate(@"^\d+$");

    /// <summary>
    /// 快速验证一个字符串是否符合指定正则表达式
    /// </summary>
    /// <param name="value">需验证的字符串</param>
    /// <param name="express">正则表达式的内容</param>
    /// <returns>是否合法的bool值</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static bool Validate(this string value, string express)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var myRegex = new Regex(express);
        return myRegex.IsMatch(value);
    }

    /// <summary>
    /// 从字符串的开头得到一个字符串的子串 len参数不能大于给定字符串的长度
    /// </summary>
    /// <param name="str"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string Left(this string str, int len) => str.Length < len ? throw new ArgumentException("len参数不能大于给定字符串的长度") : str[..len];

    /// <summary>
    /// 从字符串的末尾得到一个字符串的子串 len参数不能大于给定字符串的长度
    /// </summary>
    /// <param name="str"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string Right(this string str, int len) => str.Length < len ? throw new ArgumentException("len参数不能大于给定字符串的长度") : str.Substring(str.Length - len, len);

    /// <summary>
    /// len参数大于给定字符串是返回原字符串
    /// </summary>
    /// <param name="str"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    public static string MaxLeft(this string str, int len) => str.Length < len ? str : str[..len];

    /// <summary>
    /// 从字符串的末尾得到一个字符串的子串
    /// </summary>
    /// <param name="str"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    public static string MaxRight(this string str, int len) => str.Length < len ? str : str.Substring(str.Length - len, len);

    /// <summary>
    /// 字符串掩码[俗称:脱敏]
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="mask">掩码符</param>
    /// <returns></returns>
    public static string Mask(this string value, char mask = '*')
    {
        if (string.IsNullOrWhiteSpace(value.Trim())) return value;
        value = value.Trim();
        var masks = mask.ToString().PadLeft(4, mask);
        return value.Length switch
        {
            >= 11 => MaskElevenRegex().Replace(value, $"$1{masks}$2"),
            10 => MaskTenRegex().Replace(value, $"$1{masks}$2"),
            9 => MaskNineRegex().Replace(value, $"$1{masks}$2"),
            8 => MaskEightRegex().Replace(value, $"$1{masks}$2"),
            7 => MaskSevenRegex().Replace(value, $"$1{masks}$2"),
            6 => MaskSixRegex().Replace(value, $"$1{masks}$2"),
            _ => MaskLessThanSixRegex().Replace(value, $"$1{masks}")
        };
    }

    [GeneratedRegex("(.{3}).*(.{4})")]
    private static partial Regex MaskElevenRegex();

    [GeneratedRegex("(.{3}).*(.{3})")]
    private static partial Regex MaskTenRegex();

    [GeneratedRegex("(.{2}).*(.{3})")]
    private static partial Regex MaskNineRegex();

    [GeneratedRegex("(.{2}).*(.{2})")]
    private static partial Regex MaskEightRegex();

    [GeneratedRegex("(.{1}).*(.{2})")]
    private static partial Regex MaskSevenRegex();

    [GeneratedRegex("(.{1}).*(.{1})")]
    private static partial Regex MaskSixRegex();

    [GeneratedRegex("(.{1}).*")]
    private static partial Regex MaskLessThanSixRegex();

    /// <summary>
    /// 根据正则替换
    /// </summary>
    /// <param name="input"></param>
    /// <param name="regex">正则表达式</param>
    /// <param name="replacement">新内容</param>
    /// <returns></returns>
    public static string Replace(this string input, Regex regex, string replacement) => regex.Replace(input, replacement);

    /// <summary>
    /// 截断一个字符串,并在末尾添加一个后缀
    /// </summary>
    /// <param name="value">原始字符串</param>
    /// <param name="maxLength">最大长度(添加后缀后的长度)</param>
    /// <param name="suffix">后缀,默认: ...</param>
    /// <returns></returns>
    public static string Truncate(this string value, int maxLength, string suffix = "...") =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : maxLength - suffix.Length <= 0
                ? suffix[..maxLength]
                : $"{value[..(maxLength - suffix.Length)]}{suffix}";

    /// <summary>
    /// 将字符串集合链接起来
    /// </summary>
    /// <param name="strs"></param>
    /// <param name="separate">分隔符</param>
    /// <param name="removeEmpty">是否移除空白字符</param>
    /// <returns></returns>
    public static string Join(this IEnumerable<string> strs, string separate = ", ", bool removeEmpty = false) => string.Join(separate, removeEmpty ? strs.Where(s => !string.IsNullOrEmpty(s)) : strs);

    /// <summary>
    /// 转成非null
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string AsNotNull(this string? s) => string.IsNullOrEmpty(s) ? "" : s;

    /// <summary>
    /// 转成非null
    /// </summary>
    /// <param name="s"></param>
    /// <param name="value">为空时的替换值</param>
    /// <returns></returns>
    public static string IfNullOrEmpty(this string s, string value) => string.IsNullOrEmpty(s) ? value : s;

    /// <summary>
    /// 转成非null
    /// </summary>
    /// <param name="s"></param>
    /// <param name="valueFactory">为空时的替换值函数</param>
    /// <returns></returns>
    public static string IfNullOrEmpty(this string s, Func<string> valueFactory) => string.IsNullOrEmpty(s) ? valueFactory() : s;

    /// <summary>
    /// 匹配手机号码
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <returns>是否匹配成功</returns>
    public static bool MatchPhoneNumber(this string s) => !string.IsNullOrEmpty(s) && s[0] == '1' && (s[1] > '2' || s[1] <= '9');

    /// <summary>
    /// 转换人民币大小金额 .
    /// </summary>
    /// <param name="numStr">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRmb(this string numStr) => numStr.ConvertTo<decimal>().ToRmb();

    /// <summary>
    /// 将格式化日期串转化为相应的日期
    /// （比如2004/05/06，2004-05-06 12:00:03，12:23:33.333等）
    /// </summary>
    /// <param name="value">日期格式化串</param>
    /// <returns>转换后的日期，对于不能转化的返回DateTime.MinValue</returns>
    public static DateTime ToDateTime(this string value) => value.ToDateTime(DateTime.MinValue);

    /// <summary>
    /// 将格式化日期串转化为相应的日期
    /// （比如2004/05/06，2004-05-06 12:00:03，12:23:33.333等）
    /// </summary>
    /// <param name="value">日期格式化串</param>
    /// <param name="defaultValue">当为空或错误时的返回日期</param>
    /// <returns>转换后的日期</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static DateTime ToDateTime(this string value, DateTime defaultValue)
    {
        var result = DateTime.MinValue;
        return string.IsNullOrEmpty(value) || DateTime.TryParse(value, out result) ? result : defaultValue;
    }

    /// <summary>
    /// 从字符串获取DateTime?,支持的字符串格式:'2020/10/01,2020-10-01,20201001,2020.10.01'
    /// </summary>
    /// <param name="value"></param>
    /// <param name="force">true:当无法转换成功时抛出异常.false:当无法转化成功时返回null</param>
    public static DateTime? ToDateTime(this string value, bool force)
    {
        value = ToDateTimeRegex().Replace(value, "-");
        if (value.Split('-').Length == 1 && value.Length == 8)
        {
            value = string.Join("-", value[..4], value.Substring(4, 2), value.Substring(6, 2));
        }
        return DateTime.TryParse(value, out var date)
                   ? date
                   : force
                       ? throw new("string format is not correct,must like:2020/10/01,2020-10-01,20201001,2020.10.01")
                       : null;
    }

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex ToDateTimeRegex();

    /// <summary>
    /// 将字符串转化为固定日期格式字符串,如:20180506 --> 2018-05-06
    /// </summary>
    /// <exception cref="FormatException"></exception>
    public static string ToDateTimeFormat(this string value, bool force = true)
    {
        var sb = new StringBuilder(value);
        sb.Replace("/", "-").Replace(".", "-").Replace("。", "-").Replace(",", "-").Replace(" ", "-").Replace("|", "-");
        if (sb.ToString().Split('-').Length == 1 && sb.Length == 8) sb = sb.Insert(4, "-").Insert(7, "-");
        return DateTime.TryParse(sb.ToString(), out _)
                   ? sb.ToString()
                   : force
                       ? throw new ArgumentException("string format is not correct,must like:2020/10/01,2020-10-01,20201001,2020.10.01")
                       : string.Empty;
    }

    /// <summary>
    /// 将字符串转化为内存字节流
    /// </summary>
    /// <param name="value">需转换的字符串</param>
    /// <param name="encoding">编码类型</param>
    /// <returns>字节流</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static MemoryStream ToStream(this string value, Encoding encoding)
    {
        var mStream = new MemoryStream();
        var data = encoding.GetBytes(value);
        mStream.Write(data, 0, data.Length);
        mStream.Position = 0;
        return mStream;
    }

    /// <summary>
    /// 将字符串转化为内存字节流
    /// </summary>
    /// <param name="value">需转换的字符串</param>
    /// <param name="charset">字符集代码</param>
    /// <returns>字节流</returns>
    public static MemoryStream ToStream(this string value, string charset) => value.ToStream(Encoding.GetEncoding(charset));

    /// <summary>
    /// 将字符串以默认编码转化为内存字节流
    /// </summary>
    /// <param name="value">需转换的字符串</param>
    /// <returns>字节流</returns>
    public static MemoryStream ToStream(this string value) => value.ToStream(Encoding.UTF8);

    /// <summary>
    /// 转换为Guid类型
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Guid ToGuid(this string str) => Guid.TryParse(str, out var guid) ? guid : Guid.Empty;

    /// <summary>
    /// 转全角的函数(SBC case)
    /// </summary>
    /// <param name="input">需要转换的字符串</param>
    /// <returns>转换为全角的字符串</returns>
    public static string ToSBC(this string input)
    {
        //半角转全角：
        var c = input.ToCharArray();
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] == 0x20)
            {
                c[i] = (char)0x3000;
                continue;
            }
            if (c[i] < 0x7F) c[i] = (char)(c[i] + 0xFEE0);
        }
        return new(c);
    }

    /// <summary>
    /// 转半角的函数(SBC case)
    /// </summary>
    /// <param name="input">需要转换的字符串</param>
    /// <returns>转换为半角的字符串</returns>
    public static string ToDBC(this string input)
    {
        var c = input.ToCharArray();
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] == 0x3000)
            {
                c[i] = (char)0x20;
                continue;
            }
            if (c[i] is > (char)0xFF00 and < (char)0xFF5F)
            {
                c[i] = (char)(c[i] - 0xFEE0);
            }
        }
        return new(c);
    }

    /// <summary>
    /// 使用指针的方式反转字符串,该函数会修改原字符串.
    /// </summary>
    /// <param name="value">待反转字符串</param>
    public static unsafe void Reverse(this string value)
    {
        fixed (char* pText = value)
        {
            var pStart = pText;
            var pEnd = pText + value.Length - 1;
            while (pStart < pEnd)
            {
                var temp = *pStart;
                *pStart++ = *pEnd;
                *pEnd-- = temp;
            }
        }
    }

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词(快速匹配)
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool Contains(this string source, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not string[] array)
        {
            array = keys.ToArray();
        }
        return array.Length != 0 && !string.IsNullOrEmpty(source) && (ignoreCase ? array.Any(item => source.Contains(item, StringComparison.InvariantCultureIgnoreCase)) : array.Any(source.Contains));
    }

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词(安全匹配)
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool ContainsSafety(this string source, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not string[] array)
        {
            array = keys.ToArray();
        }
        if (array.Length == 0 || string.IsNullOrEmpty(source))
            return false;
        var flag = false;
        if (ignoreCase)
        {
            foreach (var item in array)
            {
                if (source.Contains(item)) flag = true;
            }
        }
        else
        {
            foreach (var item in array)
            {
                if (source?.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) >= 0) flag = true;
            }
        }
        return flag;
    }

    /// <summary>
    /// 检测字符串中是否以列表中的关键词结尾
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool EndsWith(this string source, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not string[] array)
        {
            array = keys.ToArray();
        }
        if (array.Length == 0 || string.IsNullOrEmpty(source))
            return false;
        var pattern = $"({array.Select(Regex.Escape).Join("|")})$";
        return ignoreCase ? Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(source, pattern);
    }

    /// <summary>
    /// 检测字符串中是否以列表中的关键词开始
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool StartsWith(this string source, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not string[] array)
        {
            array = keys.ToArray();
        }
        if (array.Length == 0 || string.IsNullOrEmpty(source))
            return false;
        var pattern = $"^({array.Select(Regex.Escape).Join("|")})";
        return ignoreCase ? Regex.IsMatch(source, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(source, pattern);
    }

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="regex">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool RegexMatch(this string source, string regex, bool ignoreCase = true) => !string.IsNullOrEmpty(regex) && !string.IsNullOrEmpty(source) && (ignoreCase ? Regex.IsMatch(source, regex, RegexOptions.IgnoreCase) : Regex.IsMatch(source, regex));

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <param name="regex">关键词列表</param>
    /// <returns></returns>
    public static bool RegexMatch(this string source, Regex regex) => !string.IsNullOrEmpty(source) && regex.IsMatch(source);

    /// <summary>
    /// 尝试将十六进制字符串解析为字节数组
    /// </summary>
    /// <param name="hex">十六进制字符串</param>
    /// <param name="bytes">A byte array.</param>
    /// <returns>如果成功解析十六进制字符串，则为 True</returns>
    public static bool TryParseHex(this string hex, out byte[]? bytes)
    {
        bytes = null;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var buffer = new byte[(hex.Length + 1) / 2];
        var i = 0;
        var j = 0;
        if (hex.Length % 2 == 1)
        {
            // if s has an odd length assume an implied leading "0"
            if (!TryParseHex(hex[i++], out var y)) return false;
            buffer[j++] = (byte)y;
        }
        while (i < hex.Length)
        {
            if (!TryParseHex(hex[i++], out var x)) return false;
            if (!TryParseHex(hex[i++], out var y)) return false;
            buffer[j++] = (byte)((x << 4) | y);
        }
        bytes = buffer;
        return true;
    }

    private static bool TryParseHex(char c, out int value)
    {
        switch (c)
        {
            case >= '0' and <= '9':
                value = c - '0';
                return true;
            case >= 'a' and <= 'f':
                value = 10 + (c - 'a');
                return true;
            case >= 'A' and <= 'F':
                value = 10 + (c - 'A');
                return true;
            default:
                value = 0;
                return false;
        }
    }

    /// <summary>
    /// 获取16位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string To16MD5(this string value) => value.To32MD5().Substring(8, 16);

    /// <summary>
    /// 获取32位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string To32MD5(this string value)
    {
        // 计算所需的最大字节数
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        var utf8Bytes = maxByteCount <= 256 ? stackalloc byte[maxByteCount] : new byte[maxByteCount];
        Span<byte> hashBytes = stackalloc byte[MD5.HashSizeInBytes];
        // 将字符串编码为 UTF-8 字节
        var byteCount = Encoding.UTF8.GetBytes(value, utf8Bytes);
        // 计算 MD5 哈希
        MD5.HashData(utf8Bytes[..byteCount], hashBytes);
        // 将哈希字节转换为十六进制字符串
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// 将小驼峰命名转为大驼峰命名,如: FirstName
    /// </summary>
    public static string ToUpperCamelCase(this string value)
    {
        return value.Length > 0 && char.IsLower(value[0])
                   ? string.Create(value.Length, value, static (newSpan, originString) =>
                   {
                       originString.CopyTo(newSpan);
                       newSpan[0] = char.ToUpperInvariant(originString[0]);
                   })
                   : value;
    }

    /// <summary>
    /// 将大驼峰命名转为小驼峰命名,如: firstName
    /// </summary>
    public static string ToLowerCamelCase(this string value)
    {
        return value.Length > 0 && char.IsUpper(value[0])
                   ? string.Create(value.Length, value, static (newSpan, originString) =>
                   {
                       originString.CopyTo(newSpan);
                       newSpan[0] = char.ToLowerInvariant(originString[0]);
                   })
                   : value;
    }

    /// <summary>
    /// 将大(小)驼峰命名转为蛇形命名,如: first_name
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        var builder = new StringBuilder();
        var previousUpper = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && !previousUpper)
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(c));
                previousUpper = true;
            }
            else
            {
                builder.Append(c);
                previousUpper = false;
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 将蛇形命名转化成驼峰命名
    /// </summary>
    /// <param name="value"></param>
    /// <param name="toType">目标方式,默认:小驼峰</param>
    /// <returns></returns>
    public static string SnakeCaseToCamelCase(this string value, ECamelCase toType = ECamelCase.LowerCamelCase)
    {
        if (string.IsNullOrEmpty(value)) return value;
        // 分割字符串，然后将每个单词(除了第一个)的首字母大写
        var words = value.Split('_');
        var index = 0;
        if (toType is ECamelCase.LowerCamelCase)
        {
            words[0] = words[0].ToLowerCamelCase();
            index = 1;
        }
        for (; index < words.Length; index++)
        {
            if (words[index].Length > 0)
            {
                words[index] = words[index].ToUpperCamelCase();
            }
        }
        return string.Concat(words);
    }

    /// <summary>
    /// 获取一个能在控制台中点击的路径,使用 [<see langword="Ctrl + 鼠标左键" />] 点击打开对应目录
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    /// @"F:\tools\test\test\bin\Release\net9.0\win-x64\publish".GetClickablePath();
    /// Output:
    ///   F:\tools\test\test\bin\Release\net9.0\win-x64\publish 
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="path">需要处理的路径</param>
    /// <returns></returns>
    public static string GetClickablePath(this string path) => $"\e]8;;file://\e\\{path}\e]8;;\e\\";

    /// <summary>
    /// 获取一个能在控制台中点击的相对路径,使用 [<see langword="Ctrl + 鼠标左键" />] 点击打开对应目录
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    /// @"F:\tools\test\test\bin\Release\net9.0\win-x64\publish".GetClickableRelativePath();
    /// Output:
    ///   bin\Release\net9.0\win-x64\publish
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="path">需要处理的路径</param>
    /// <param name="maxDeep">仅保留最后 N 层目录,默认5层,当超过最大层数后显示全路径</param>
    /// <returns></returns>
    public static string GetClickableRelativePath(this string path, int maxDeep = 5)
    {
        // 分割路径并仅保留最后 N 层目录
        var pathParts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var displayPath = pathParts.Length > maxDeep ? string.Join(Path.DirectorySeparatorChar, pathParts[^maxDeep..]) : path;
        return $"\e]8;;file://{path}\e\\{displayPath}\e]8;;\e\\";
    }
}