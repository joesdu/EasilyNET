using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Extensions.BaseType;

/// <summary>
/// 字符串String扩展
/// </summary>
public static class StringExtension
{
    #region 以特定字符串间隔的字符串转化为字符串集合

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
            _ = pos >= 0 ? col.Add(value[index..pos]) : col.Add(value[index..]);
            index = pos + len;
        }
        return col;
    }

    #endregion

    #region 将字符串中的单词首字母大写或者小写

    /// <summary>
    /// 将字符串中的单词首字母大写或者小写
    /// </summary>
    /// <param name="value">单词</param>
    /// <param name="lower">是否小写? 默认:true</param>
    /// <returns></returns>
    public static string ToTitleUpperCase(this string value, bool lower = true)
    {
#pragma warning disable SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
        var regex = new Regex(@"\w+");
#pragma warning restore SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
        return regex.Replace(value,
            delegate(Match m)
            {
                var str = m.ToString();
                if (!char.IsLower(str[0])) return str;
                var header = lower ? char.ToLower(str[0], CultureInfo.CurrentCulture) : char.ToUpper(str[0], CultureInfo.CurrentCulture);
                return $"{header}{str[1..]}";
            });
    }

    #endregion

    #region 字符串插入指定分隔符

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

    #endregion

    #region 检查一个字符串是否是纯数字构成的，一般用于查询字符串参数的有效性验证

    /// <summary>
    /// 检查一个字符串是否是纯数字构成的,一般用于查询字符串参数的有效性验证
    /// </summary>
    /// <param name="value">需验证的字符串</param>
    /// <returns>是否合法的bool值</returns>
    public static bool IsNumber(this string value) => value.Validate(@"^\d+$");

    #endregion

    #region 验证一个字符串是否符合指定的正则表达式

    /// <summary>
    /// 快速验证一个字符串是否符合指定的正则表达式
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

    #endregion

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
#pragma warning disable SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
        return value.Length switch
        {
            >= 11 => Regex.Replace(value, "(.{3}).*(.{4})", $"$1{masks}$2"),
            10    => Regex.Replace(value, "(.{3}).*(.{3})", $"$1{masks}$2"),
            9     => Regex.Replace(value, "(.{2}).*(.{3})", $"$1{masks}$2"),
            8     => Regex.Replace(value, "(.{2}).*(.{2})", $"$1{masks}$2"),
            7     => Regex.Replace(value, "(.{1}).*(.{2})", $"$1{masks}$2"),
            6     => Regex.Replace(value, "(.{1}).*(.{1})", $"$1{masks}$2"),
            _     => Regex.Replace(value, "(.{1}).*", $"$1{masks}")
        };
#pragma warning restore SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
    }

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
    /// 将JSON字符串符合条件的字段,按照最大长度截断
    /// </summary>
    /// <param name="json"></param>
    /// <param name="predicate">筛选字段名</param>
    /// <param name="maxLength">最大长度(添加后缀后的长度)</param>
    /// <param name="suffix">后缀,默认: ...</param>
    /// <returns></returns>
    public static string TruncateJson(this string json, Func<string, bool> predicate, int maxLength, string suffix = "...")
    {
        // Parse the json string into a JsonDocument
        using var doc = JsonDocument.Parse(json);
        // Get the root element of the document
        var root = doc.RootElement;
        // Create a StringBuilder to store the truncated json string
        var sb = new StringBuilder();
        // Write the opening brace of the object
        sb.Append('{');
        // Loop through all the properties in the root element
        foreach (var property in root.EnumerateObject())
        {
            // Get the name and value of the property as strings
            var name = property.Name;
            var value = property.Value.ToString();
            // If the predicate function returns true for the property name, and the value is longer than the max length, truncate it and add a suffix
            if (predicate(name) && value.Length > maxLength)
            {
                value = value.Truncate(maxLength - suffix.Length, suffix);
            }
            // Write the property name and value to the StringBuilder with quotes and comma
            sb.Append($"\"{name}\":\"{value}\",");
        }
        // Remove the last comma from the StringBuilder
        sb.Length--;
        // Write the closing brace of the object
        sb.Append('}');
        // Return the truncated json string
        return sb.ToString();
    }

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
    public static string AsNotNull(this string s) => string.IsNullOrEmpty(s) ? "" : s;

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

    #region 校验手机号码的正确性

    /// <summary>
    /// 匹配手机号码
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <returns>是否匹配成功</returns>
    public static bool MatchPhoneNumber(this string s) => !string.IsNullOrEmpty(s) && s[0] == '1' && (s[1] > '2' || s[1] <= '9');

    #endregion 校验手机号码的正确性

    #region 字符串转为日期

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
        value = value.Replace("/", "-").Replace(".", "-").Replace("。", "-").Replace(",", "-").Replace(" ", "-").Replace("|", "-");
        if (value.Split('-').Length == 1 && value.Length == 8) value = string.Join("-", value[..4], value.Substring(4, 2), value.Substring(6, 2));
        return DateTime.TryParse(value, out var date) switch
        {
            false => force ? throw new("string format is not correct,must like:2020/10/01,2020-10-01,20201001,2020.10.01") : null,
            _     => date
        };
    }

    /// <summary>
    /// 将字符串转化为固定日期格式字符串,如:20180506 --> 2018-05-06
    /// </summary>
    /// <exception cref="FormatException"></exception>
    public static string ToDateTimeFormat(this string value, bool force = true)
    {
        value = value.Replace("/", "-").Replace(".", "-").Replace("。", "-").Replace(",", "-").Replace(" ", "-").Replace("|", "-");
        if (value.Split('-').Length == 1 && value.Length == 8) value = string.Join("-", value[..4], value.Substring(4, 2), value.Substring(6, 2));
        return DateTime.TryParse(value, out _)
                   ? value
                   : force
                       ? throw new("string format is not correct,must like:2020/10/01,2020-10-01,20201001,2020.10.01")
                       : string.Empty;
    }
#if !NETSTANDARD
    /// <summary>
    /// 获取某个日期串的DateOnly
    /// </summary>
    /// <param name="value">格式如: 2022-02-28</param>
    /// <returns></returns>
    public static DateOnly ToDateOnly(this string value) => DateOnly.FromDateTime(value.ToDateTime());

    /// <summary>
    /// 获取某个时间串的TimeOnly
    /// </summary>
    /// <param name="value">格式如: 23:20:10</param>
    /// <returns></returns>
    public static TimeOnly ToTimeOnly(this string value) => TimeOnly.FromDateTime($"{DateTime.Now:yyyy-MM-dd} {value}".ToDateTime());
#endif

    #endregion

    #region 将字符串转为整数,数组,内存流,GUID(GUID需要字符串本身为GUID格式)

    /// <summary>
    /// 将字符串转化为内存字节流
    /// </summary>
    /// <param name="value">需转换的字符串</param>
    /// <param name="encoding">编码类型</param>
    /// <returns>字节流</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static MemoryStream ToStream(this string value, Encoding encoding)
    {
        using var mStream = new MemoryStream();
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
    /// 将字符串拆分为数组
    /// </summary>
    /// <param name="value">需转换的字符串</param>
    /// <param name="separator">分割符</param>
    /// <returns>字符串数组</returns>
    public static string[] Split(this string value, string separator)
    {
        var collection = value.ToStringCollection(separator);
        var vs = new string[collection.Count];
        collection.CopyTo(vs, 0);
        return vs;
    }

    /// <summary>
    /// 转换为Guid类型
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Guid ToGuid(this string str) => Guid.TryParse(str, out var guid) ? guid : Guid.Empty;

    #endregion

    #region Base64-String互转

    /// <summary>
    /// 将字符串转换成Base64字符串
    /// </summary>
    /// <param name="value">字符串</param>
    /// <returns></returns>
    public static string ToBase64(this string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    /// <summary>
    /// 将Base64字符转成String
    /// </summary>
    /// <param name="value">Base64字符串</param>
    /// <returns></returns>
    public static string Base64ToString(this string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

    #endregion

    #region 半角全角相互转换

    /// <summary>
    /// 转全角的函数(SBC case)
    /// </summary>
    /// <param name="input">需要转换的字符串</param>
    /// <returns>转换为全角的字符串</returns>
    public static string ToSbc(this string input)
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
    public static string ToDbc(this string input)
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

    #endregion

    #region 字符串反转

    /// <summary>
    /// 使用指针的方式反转字符串,该函数会修改原字符串.
    /// </summary>
    /// <param name="value">待反转字符串</param>
    /// <returns>反转后的结果</returns>
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
    /// 使用StringBuilder和索引器的方式反转字符串,该函数不会修改原字符串
    /// </summary>
    /// <param name="value">待反转字符串</param>
    /// <returns>反转后的结果</returns>
    public static string ReverseByStringBuilder(this string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = value.Length; i > 0;)
        {
            _ = sb.Append(value[--i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 使用Array.Reverse()的方式反转字符串,该函数不会修改原字符串
    /// </summary>
    /// <param name="value">待反转字符串</param>
    /// <returns>反转后的结果</returns>
    public static string ReverseByArray(this string value)
    {
        var arr = value.ToCharArray();
        Array.Reverse(arr);
        return new(arr);
    }

    #endregion

    #region 检测字符串中是否包含列表中的关键词

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词(快速匹配)
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool Contains(this string s, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not ICollection<string> array)
        {
            array = keys.ToArray();
        }
        return array.Count != 0 && !string.IsNullOrEmpty(s) && (ignoreCase ? array.Any(item => s.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) >= 0) : array.Any(s.Contains));
    }

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词(安全匹配)
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool ContainsSafety(this string s, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not ICollection<string> array)
        {
            array = keys.ToArray();
        }
        if (array.Count == 0 || string.IsNullOrEmpty(s))
            return false;
        var flag = false;
        if (ignoreCase)
        {
            foreach (var item in array)
            {
                if (s.Contains(item)) flag = true;
            }
        }
        else
        {
            foreach (var item in array)
            {
                if (s?.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) >= 0) flag = true;
            }
        }
        return flag;
    }

    /// <summary>
    /// 检测字符串中是否以列表中的关键词结尾
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool EndsWith(this string s, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not ICollection<string> array)
        {
            array = keys.ToArray();
        }
        if (array.Count == 0 || string.IsNullOrEmpty(s))
            return false;
        var pattern = $"({array.Select(Regex.Escape).Join("|")})$";
        return ignoreCase ? Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(s, pattern);
    }

    /// <summary>
    /// 检测字符串中是否以列表中的关键词开始
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <param name="keys">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool StartsWith(this string s, IEnumerable<string> keys, bool ignoreCase = true)
    {
        if (keys is not ICollection<string> array)
        {
            array = keys.ToArray();
        }
        if (array.Count == 0 || string.IsNullOrEmpty(s))
            return false;
        var pattern = $"^({array.Select(Regex.Escape).Join("|")})";
        return ignoreCase ? Regex.IsMatch(s, pattern, RegexOptions.IgnoreCase) : Regex.IsMatch(s, pattern);
    }

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <param name="regex">关键词列表</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public static bool RegexMatch(this string s, string regex, bool ignoreCase = true)
    {
#pragma warning disable IDE0046
        if (string.IsNullOrEmpty(regex) || string.IsNullOrEmpty(s))
#pragma warning restore IDE0046
            return false;
        return ignoreCase ? Regex.IsMatch(s, regex, RegexOptions.IgnoreCase) : Regex.IsMatch(s, regex);
    }

    /// <summary>
    /// 检测字符串中是否包含列表中的关键词
    /// </summary>
    /// <param name="s">源字符串</param>
    /// <param name="regex">关键词列表</param>
    /// <returns></returns>
    public static bool RegexMatch(this string s, Regex regex) => !string.IsNullOrEmpty(s) && regex.IsMatch(s);

    #endregion 检测字符串中是否包含列表中的关键词
}