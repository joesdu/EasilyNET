using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EasilyNET.Core.Commons;
using EasilyNET.Core.Enums;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">String extensions</para>
///     <para xml:lang="zh">字符串扩展</para>
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Determines whether the string value is not null, empty, or consists only of white-space characters.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the string value is not null, not empty, and does not consist solely of
    /// white-space characters; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsNotNullOrWhiteSpace([NotNullWhen(true)] this string? val) => !string.IsNullOrWhiteSpace(val);

    /// <summary>
    /// Determines whether the string is not null or empty.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the string is not null or empty; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsNotNullOrEmpty([NotNullWhen(true)] this string? val) => !string.IsNullOrEmpty(val);

    [GeneratedRegex(@"\s")]
    private static partial Regex RemoveWhiteSpaceRegex();

    [GeneratedRegex(@"\w+")]
    private static partial Regex ToTitleUpperCaseRegex();

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
    ///     <para xml:lang="en">Join a collection of strings</para>
    ///     <para xml:lang="zh">将字符串集合链接起来</para>
    /// </summary>
    /// <param name="strs"></param>
    /// <param name="separate">
    ///     <para xml:lang="en">Separator</para>
    ///     <para xml:lang="zh">分隔符</para>
    /// </param>
    /// <param name="removeEmpty">
    ///     <para xml:lang="en">Remove empty characters</para>
    ///     <para xml:lang="zh">是否移除空白字符</para>
    /// </param>
    public static string Join(this IEnumerable<string> strs, char separate = ',', bool removeEmpty = false) => string.Join(separate, removeEmpty ? strs.Where(s => !string.IsNullOrWhiteSpace(s)) : strs);

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

    /// <param name="value">
    ///     <para xml:lang="en">Word</para>
    ///     <para xml:lang="zh">单词</para>
    /// </param>
    extension(string value)
    {
        /// <summary>
        ///     <para xml:lang="en">Capitalize or lowercase the first letter of each word in the string</para>
        ///     <para xml:lang="zh">将字符串中的单词首字母大写或者小写</para>
        /// </summary>
        /// <param name="lower">
        ///     <para xml:lang="en">Lowercase? Default: true</para>
        ///     <para xml:lang="zh">是否小写? 默认:true</para>
        /// </param>
        public string ToTitleUpperCase(bool lower = true)
        {
            var regex = ToTitleUpperCaseRegex();
            return regex.Replace(value,
                delegate(Match m)
                {
                    var str = m.ToString();
                    if (!char.IsLower(str[0]))
                    {
                        return str;
                    }
                    var header = lower ? char.ToLower(str[0], CultureInfo.CurrentCulture) : char.ToUpper(str[0], CultureInfo.CurrentCulture);
                    return $"{header}{str[1..]}";
                });
        }

        /// <summary>
        ///     <para xml:lang="en">Simplify and optimize the String.Format method</para>
        ///     <para xml:lang="zh">简化和优化String.Format方法</para>
        /// </summary>
        /// <example>
        ///     <code>
        ///   <![CDATA[
        /// var str = "{0} is {1} years old.";
        ///   var result = str.Format("Alice", 30);
        ///   Console.WriteLine(result);
        /// Output:
        ///   Alice is 30 years old.
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="args">
        ///     <para xml:lang="en">An array of objects containing zero or more objects to format</para>
        ///     <para xml:lang="zh">一个对象数组，包含零个或多个要格式化的对象</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Formatted string. If the format string is empty or contains only whitespace characters, an empty string is returned</para>
        ///     <para xml:lang="zh">格式化后的字符串。如果格式字符串为空或仅包含空白字符，则返回空字符串</para>
        /// </returns>
        public string Format(params object?[] args)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            var str = FormattableStringFactory.Create(value, args);
            return str.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     <para xml:lang="en">Remove all whitespace characters from the string</para>
        ///     <para xml:lang="zh">移除字符串中所有空白符</para>
        /// </summary>
        public string RemoveWhiteSpace() => RemoveWhiteSpaceRegex().Replace(value, string.Empty);

        /// <summary>
        ///     <para xml:lang="en">Convert a string with specific character intervals to a string collection</para>
        ///     <para xml:lang="zh">以特定字符间隔的字符串转化为字符串集合</para>
        /// </summary>
        /// <param name="separator">
        ///     <para xml:lang="en">Separator for substrings in this instance</para>
        ///     <para xml:lang="zh">分隔此实例中子字符串</para>
        /// </param>
        public StringCollection ToStringCollection(string separator)
        {
            var col = new StringCollection();
            if (string.IsNullOrWhiteSpace(separator) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(value.Trim()))
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
        ///     <para xml:lang="en">Insert a specified separator into the string</para>
        ///     <para xml:lang="zh">字符串插入指定分隔符</para>
        /// </summary>
        /// <param name="spacingString">
        ///     <para xml:lang="en">Separator</para>
        ///     <para xml:lang="zh">分隔符</para>
        /// </param>
        /// <param name="spacingIndex">
        ///     <para xml:lang="en">Insert separator every few characters</para>
        ///     <para xml:lang="zh">隔多少个字符插入分隔符</para>
        /// </param>
        public string Spacing(string spacingString, int spacingIndex)
        {
            var sb = new StringBuilder(value);
            for (var i = spacingIndex; i <= sb.Length; i += spacingIndex + 1)
            {
                if (i >= sb.Length)
                {
                    break;
                }
                _ = sb.Insert(i, spacingString);
            }
            return sb.ToString();
        }

        /// <summary>
        ///     <para xml:lang="en">Check if a string is composed of pure numbers, generally used for validity verification of query string parameters</para>
        ///     <para xml:lang="zh">检查一个字符串是否是纯数字构成的,一般用于查询字符串参数的有效性验证</para>
        /// </summary>
        public bool IsNumber() => value.Validate(@"^\d+$");

        /// <summary>
        ///     <para xml:lang="en">Quickly verify if a string matches a specified regular expression</para>
        ///     <para xml:lang="zh">快速验证一个字符串是否符合指定正则表达式</para>
        /// </summary>
        /// <param name="express">
        ///     <para xml:lang="en">Content of the regular expression</para>
        ///     <para xml:lang="zh">正则表达式的内容</para>
        /// </param>
        public bool Validate(string express)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            var myRegex = new Regex(express);
            return myRegex.IsMatch(value);
        }

        /// <summary>
        ///     <para xml:lang="en">Get a substring from the beginning of a string, the len parameter cannot be greater than the length of the given string</para>
        ///     <para xml:lang="zh">从字符串的开头得到一个字符串的子串 len参数不能大于给定字符串的长度</para>
        /// </summary>
        /// <param name="len"></param>
        /// <exception cref="ArgumentException"></exception>
        public string Left(int len) => value.Length < len ? throw new ArgumentException("len参数不能大于给定字符串的长度") : value[..len];

        /// <summary>
        ///     <para xml:lang="en">Get a substring from the end of a string, the len parameter cannot be greater than the length of the given string</para>
        ///     <para xml:lang="zh">从字符串的末尾得到一个字符串的子串 len参数不能大于给定字符串的长度</para>
        /// </summary>
        /// <param name="len"></param>
        /// <exception cref="ArgumentException"></exception>
        public string Right(int len) => value.Length < len ? throw new ArgumentException("len参数不能大于给定字符串的长度") : value.Substring(value.Length - len, len);

        /// <summary>
        ///     <para xml:lang="en">Return the original string if the len parameter is greater than the given string</para>
        ///     <para xml:lang="zh">len参数大于给定字符串是返回原字符串</para>
        /// </summary>
        /// <param name="len"></param>
        public string MaxLeft(int len) => value.Length < len ? value : value[..len];

        /// <summary>
        ///     <para xml:lang="en">Get a substring from the end of a string</para>
        ///     <para xml:lang="zh">从字符串的末尾得到一个字符串的子串</para>
        /// </summary>
        /// <param name="len"></param>
        public string MaxRight(int len) => value.Length < len ? value : value.Substring(value.Length - len, len);

        /// <summary>
        ///     <para xml:lang="en">String masking [also known as desensitization]</para>
        ///     <para xml:lang="zh">字符串掩码[俗称:脱敏]</para>
        /// </summary>
        /// <param name="mask">
        ///     <para xml:lang="en">Mask character</para>
        ///     <para xml:lang="zh">掩码符</para>
        /// </param>
        public string Mask(char mask = '*')
        {
            if (string.IsNullOrWhiteSpace(value.Trim()))
            {
                return value;
            }
            value = value.Trim();
            var masks = mask.ToString().PadLeft(4, mask);
            return value.Length switch
            {
                >= 11 => MaskElevenRegex().Replace(value, $"$1{masks}$2"),
                10    => MaskTenRegex().Replace(value, $"$1{masks}$2"),
                9     => MaskNineRegex().Replace(value, $"$1{masks}$2"),
                8     => MaskEightRegex().Replace(value, $"$1{masks}$2"),
                7     => MaskSevenRegex().Replace(value, $"$1{masks}$2"),
                6     => MaskSixRegex().Replace(value, $"$1{masks}$2"),
                _     => MaskLessThanSixRegex().Replace(value, $"$1{masks}")
            };
        }

        /// <summary>
        ///     <para xml:lang="en">Replace based on regular expression</para>
        ///     <para xml:lang="zh">根据正则替换</para>
        /// </summary>
        /// <param name="regex">
        ///     <para xml:lang="en">Regular expression</para>
        ///     <para xml:lang="zh">正则表达式</para>
        /// </param>
        /// <param name="replacement">
        ///     <para xml:lang="en">New content</para>
        ///     <para xml:lang="zh">新内容</para>
        /// </param>
        public string Replace(Regex regex, string replacement) => regex.Replace(value, replacement);

        /// <summary>
        ///     <para xml:lang="en">Convert to non-null</para>
        ///     <para xml:lang="zh">转成非null</para>
        /// </summary>
        public string AsNotNull() => string.IsNullOrWhiteSpace(value) ? string.Empty : value;

        /// <summary>
        ///     <para xml:lang="en">Convert to non-null</para>
        ///     <para xml:lang="zh">转成非null</para>
        /// </summary>
        /// <param name="value1">
        ///     <para xml:lang="en">Replacement value when null</para>
        ///     <para xml:lang="zh">为空时的替换值</para>
        /// </param>
        public string IfNullOrEmpty(string value1) => string.IsNullOrWhiteSpace(value) ? value1 : value;

        /// <summary>
        ///     <para xml:lang="en">Convert to non-null</para>
        ///     <para xml:lang="zh">转成非null</para>
        /// </summary>
        /// <param name="valueFactory">
        ///     <para xml:lang="en">Replacement value function when null</para>
        ///     <para xml:lang="zh">为空时的替换值函数</para>
        /// </param>
        public string IfNullOrEmpty(Func<string> valueFactory) => string.IsNullOrWhiteSpace(value) ? valueFactory() : value;

        /// <summary>
        ///     <para xml:lang="en">Match phone number</para>
        ///     <para xml:lang="zh">匹配手机号码</para>
        /// </summary>
        public bool IsPhoneNumber() => !string.IsNullOrWhiteSpace(value) && value[0] == '1' && (value[1] > '2' || value[1] <= '9');

        /// <summary>
        ///     <para xml:lang="en">Convert string to DateTime, supports multiple formats</para>
        ///     <para xml:lang="zh">将字符串转化为DateTime，支持多种格式</para>
        /// </summary>
        /// <param name="force">
        ///     <para xml:lang="en">Throw exception if parsing fails</para>
        ///     <para xml:lang="zh">解析失败时是否抛出异常</para>
        /// </param>
        public DateTime? ToDateTime(bool force = false) =>
            string.IsNullOrWhiteSpace(value)
                ? force ? throw new ArgumentException("日期字符串不能为空") : null
                : DateTime.TryParseExact(value, ModuleConstants.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                    ? result
                    : force
                        ? throw new FormatException("日期字符串格式不正确")
                        : null;

        /// <summary>
        ///     <para xml:lang="en">Convert string to memory byte stream</para>
        ///     <para xml:lang="zh">将字符串转化为内存字节流</para>
        /// </summary>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding type</para>
        ///     <para xml:lang="zh">编码类型</para>
        /// </param>
        public MemoryStream ToStream(Encoding encoding)
        {
            var mStream = new MemoryStream();
            var data = encoding.GetBytes(value);
            mStream.Write(data, 0, data.Length);
            mStream.Position = 0;
            return mStream;
        }

        /// <summary>
        ///     <para xml:lang="en">Convert string to memory byte stream</para>
        ///     <para xml:lang="zh">将字符串转化为内存字节流</para>
        /// </summary>
        /// <param name="charset">
        ///     <para xml:lang="en">Character set code</para>
        ///     <para xml:lang="zh">字符集代码</para>
        /// </param>
        public MemoryStream ToStream(string charset) => value.ToStream(Encoding.GetEncoding(charset));

        /// <summary>
        ///     <para xml:lang="en">Convert string to memory byte stream with default encoding</para>
        ///     <para xml:lang="zh">将字符串以默认编码转化为内存字节流</para>
        /// </summary>
        public MemoryStream ToStream() => value.ToStream(Encoding.UTF8);

        /// <summary>
        ///     <para xml:lang="en">Convert to full-width characters (SBC case)</para>
        ///     <para xml:lang="zh">转全角的函数(SBC case)</para>
        /// </summary>
        public string ToSBC()
        {
            // 半角转全角：
            var c = value.ToCharArray();
            for (var i = 0; i < c.Length; i++)
            {
                if (c[i] == 0x20)
                {
                    c[i] = (char)0x3000;
                    continue;
                }
                if (c[i] < 0x7F)
                {
                    c[i] = (char)(c[i] + 0xFEE0);
                }
            }
            return new(c);
        }

        /// <summary>
        ///     <para xml:lang="en">Convert to half-width characters (SBC case)</para>
        ///     <para xml:lang="zh">转半角的函数(SBC case)</para>
        /// </summary>
        public string ToDBC()
        {
            var c = value.ToCharArray();
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
        ///     <para xml:lang="en">Reverse a string in a safe way, this function does not modify the original string</para>
        ///     <para xml:lang="zh">使用安全的方式反转字符串,该函数不会修改原字符串</para>
        /// </summary>
        public string Reverse()
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            var charArray = value.ToCharArray();
            Array.Reverse(charArray);
            return new(charArray);
        }

        /// <summary>
        ///     <para xml:lang="en">Check if the string contains any keywords from the list (quick match)</para>
        ///     <para xml:lang="zh">检测字符串中是否包含列表中的关键词(快速匹配)</para>
        /// </summary>
        /// <param name="keys">
        ///     <para xml:lang="en">List of keywords</para>
        ///     <para xml:lang="zh">关键词列表</para>
        /// </param>
        /// <param name="ignoreCase">
        ///     <para xml:lang="en">Ignore case</para>
        ///     <para xml:lang="zh">忽略大小写</para>
        /// </param>
        public bool Contains(IEnumerable<string> keys, bool ignoreCase = true)
        {
            if (keys is not string[] array)
            {
                array = [.. keys];
            }
            return array.Length != 0 && !string.IsNullOrWhiteSpace(value) && (ignoreCase ? array.Any(item => value.Contains(item, StringComparison.InvariantCultureIgnoreCase)) : array.Any(value.Contains));
        }

        /// <summary>
        ///     <para xml:lang="en">Check if the string contains any keywords from the list (safe match)</para>
        ///     <para xml:lang="zh">检测字符串中是否包含列表中的关键词(安全匹配)</para>
        /// </summary>
        /// <param name="keys">
        ///     <para xml:lang="en">List of keywords</para>
        ///     <para xml:lang="zh">关键词列表</para>
        /// </param>
        /// <param name="ignoreCase">
        ///     <para xml:lang="en">Ignore case</para>
        ///     <para xml:lang="zh">忽略大小写</para>
        /// </param>
        public bool ContainsSafety(IEnumerable<string> keys, bool ignoreCase = true)
        {
            if (keys is not string[] array)
            {
                array = [.. keys];
            }
            if (array.Length == 0 || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            var flag = false;
            if (ignoreCase)
            {
                foreach (var item in array)
                {
                    if (value.Contains(item))
                    {
                        flag = true;
                    }
                }
            }
            else
            {
                foreach (var item in array)
                {
                    if (value?.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        flag = true;
                    }
                }
            }
            return flag;
        }

        /// <summary>
        ///     <para xml:lang="en">Check if the string ends with any keywords from the list</para>
        ///     <para xml:lang="zh">检测字符串中是否以列表中的关键词结尾</para>
        /// </summary>
        /// <param name="keys">
        ///     <para xml:lang="en">List of keywords</para>
        ///     <para xml:lang="zh">关键词列表</para>
        /// </param>
        /// <param name="ignoreCase">
        ///     <para xml:lang="en">Ignore case</para>
        ///     <para xml:lang="zh">忽略大小写</para>
        /// </param>
        public bool EndsWith(IEnumerable<string> keys, bool ignoreCase = true)
        {
            if (keys is not string[] array)
            {
                array = [.. keys];
            }
            if (array.Length == 0 || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            var pattern = $"({array.Select(Regex.Escape).Join('|')})$";
            return ignoreCase ? Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(value, pattern);
        }

        /// <summary>
        ///     <para xml:lang="en">Check if the string starts with any keywords from the list</para>
        ///     <para xml:lang="zh">检测字符串中是否以列表中的关键词开始</para>
        /// </summary>
        /// <param name="keys">
        ///     <para xml:lang="en">List of keywords</para>
        ///     <para xml:lang="zh">关键词列表</para>
        /// </param>
        /// <param name="ignoreCase">
        ///     <para xml:lang="en">Ignore case</para>
        ///     <para xml:lang="zh">忽略大小写</para>
        /// </param>
        public bool StartsWith(IEnumerable<string> keys, bool ignoreCase = true)
        {
            if (keys is not string[] array)
            {
                array = [.. keys];
            }
            if (array.Length == 0 || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            var pattern = $"^({array.Select(Regex.Escape).Join('|')})";
            return ignoreCase ? Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(value, pattern);
        }

        /// <summary>
        ///     <para xml:lang="en">Try to parse a hexadecimal string into a byte array</para>
        ///     <para xml:lang="zh">尝试将十六进制字符串解析为字节数组</para>
        /// </summary>
        /// <param name="bytes">
        ///     <para xml:lang="en">A byte array</para>
        ///     <para xml:lang="zh">字节数组</para>
        /// </param>
        public bool TryParseHex(out byte[]? bytes)
        {
            bytes = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            var buffer = new byte[(value.Length + 1) / 2];
            var i = 0;
            var j = 0;
            if (value.Length % 2 == 1)
            {
                // if s has an odd length assume an implied leading "0"
                if (!TryParseHex(value[i++], out var y))
                {
                    return false;
                }
                buffer[j++] = (byte)y;
            }
            while (i < value.Length)
            {
                if (!TryParseHex(value[i++], out var x))
                {
                    return false;
                }
                if (!TryParseHex(value[i++], out var y))
                {
                    return false;
                }
                buffer[j++] = (byte)((x << 4) | y);
            }
            bytes = buffer;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Get a 16-character uppercase MD5 string</para>
        ///     <para xml:lang="zh">获取16位长度的MD5大写字符串</para>
        /// </summary>
        public string To16MD5() => value.To32MD5().Substring(8, 16);

        /// <summary>
        ///     <para xml:lang="en">Get a 32-character uppercase MD5 string</para>
        ///     <para xml:lang="zh">获取32位长度的MD5大写字符串</para>
        /// </summary>
        public string To32MD5()
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
        ///     <para xml:lang="en">Convert lower camel case to upper camel case, e.g., FirstName</para>
        ///     <para xml:lang="zh">将小驼峰命名转为大驼峰命名,如: FirstName</para>
        /// </summary>
        public string ToUpperCamelCase()
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
        ///     <para xml:lang="en">Convert upper camel case to lower camel case, e.g., firstName</para>
        ///     <para xml:lang="zh">将大驼峰命名转为小驼峰命名,如: firstName</para>
        /// </summary>
        public string ToLowerCamelCase()
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
        ///     <para xml:lang="en">Convert camel case to snake case, e.g., first_name</para>
        ///     <para xml:lang="zh">将大(小)驼峰命名转为蛇形命名,如: first_name</para>
        /// </summary>
        public string ToSnakeCase()
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
        ///     <para xml:lang="en">Convert snake case to camel case</para>
        ///     <para xml:lang="zh">将蛇形命名转化成驼峰命名</para>
        /// </summary>
        /// <param name="toType">
        ///     <para xml:lang="en">Target type, default: lower camel case</para>
        ///     <para xml:lang="zh">目标方式,默认:小驼峰</para>
        /// </param>
        public string SnakeCaseToCamelCase(ECamelCase toType = ECamelCase.LowerCamelCase)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
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
        ///     <para xml:lang="en">Get a clickable path in the console, use [<see langword="Ctrl + Left Click" />] to open the corresponding directory</para>
        ///     <para xml:lang="zh">获取一个能在控制台中点击的路径,使用 [<see langword="Ctrl + 鼠标左键" />] 点击打开对应目录</para>
        /// </summary>
        /// <remarks>
        ///     <code>
        ///   <![CDATA[
        /// @"F:\tools\test\test\bin\Release\net9.0\win-x64\publish".GetClickablePath();
        /// Output:
        ///   F:\tools\test\test\bin\Release\net9.0\win-x64\publish
        /// ]]>
        /// </code>
        /// </remarks>
        public string GetClickablePath()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            var fullPath = Path.GetFullPath(value);
            return TextWriterExtensions.IsAnsiSupported() ? $"\e]8;;file://\e\\{fullPath}\e]8;;\e\\" : fullPath;
        }

        /// <summary>
        ///     <para xml:lang="en">Get a clickable relative path in the console, use [<see langword="Ctrl + Left Click" />] to open the corresponding directory</para>
        ///     <para xml:lang="zh">获取一个能在控制台中点击的相对路径,使用 [<see langword="Ctrl + 鼠标左键" />] 点击打开对应目录</para>
        /// </summary>
        /// <remarks>
        ///     <code>
        ///   <![CDATA[
        /// @"F:\tools\test\test\bin\Release\net9.0\win-x64\publish".GetClickableRelativePath();
        /// Output:
        ///   bin\Release\net9.0\win-x64\publish
        /// ]]>
        /// </code>
        /// </remarks>
        /// <param name="maxDeep">
        ///     <para xml:lang="en">Only keep the last N levels of directories, default is 5 levels, show full path if it exceeds the maximum number of levels</para>
        ///     <para xml:lang="zh">仅保留最后 N 层目录,默认5层,当超过最大层数后显示全路径</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Returns content that can be directly output. If the console does not support it, the full path is displayed</para>
        ///     <para xml:lang="zh">返回可直接输出的内容.若是控制台不支持则显示全路径</para>
        /// </returns>
        public string GetClickableRelativePath(int maxDeep = 5)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            var fullPath = Path.GetFullPath(value);
            // 分割路径并仅保留最后 N 层目录
            var pathParts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var displayPath = pathParts.Length > maxDeep ? string.Join(Path.DirectorySeparatorChar, pathParts[^maxDeep..]) : fullPath;
            return TextWriterExtensions.IsAnsiSupported() ? $"\e]8;;file://{fullPath}\e\\{displayPath}\e]8;;\e\\" : fullPath;
        }

        /// <summary>
        ///     <para xml:lang="en">Simplify and optimize the String.Format method</para>
        ///     <para xml:lang="zh">简化和优化String.Format方法</para>
        /// </summary>
        /// <example>
        ///     <code>
        ///   <![CDATA[
        /// var str = "{0} is {1} years old.";
        ///   var result = str.Format(CultureInfo.InvariantCulture,"Alice", 30);
        ///   Console.WriteLine(result);
        /// Output:
        ///   Alice is 30 years old.
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="formatProvider">
        ///     <para xml:lang="en">Object used to provide specific culture format information</para>
        ///     <para xml:lang="zh">用于提供特定区域性格式信息的对象</para>
        /// </param>
        /// <param name="args">
        ///     <para xml:lang="en">An array of objects containing zero or more objects to format</para>
        ///     <para xml:lang="zh">一个对象数组，包含零个或多个要格式化的对象</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Formatted string. If the format string is empty or contains only whitespace characters, an empty string is returned</para>
        ///     <para xml:lang="zh">格式化后的字符串。如果格式字符串为空或仅包含空白字符，则返回空字符串</para>
        /// </returns>
        public string Format(IFormatProvider? formatProvider = null, params object?[] args)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            var str = FormattableStringFactory.Create(value, args);
            return str.ToString(formatProvider ?? CultureInfo.InvariantCulture);
        }
    }
}