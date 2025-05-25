/* Copyright 2010-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using EasilyNET.Core.Misc;
using EasilyNET.Core.Misc.Exceptions;

#pragma warning disable IDE0048

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">SnowId algorithm compatible with MongoDB's ObjectId, so they can be cast to each other</para>
///     <para xml:lang="zh">SnowId 算法兼容MongoDB的 ObjectId, 因此他们可以互相强制转换</para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
/// var snow_id = SnowId.GenerateNewId();
///   ]]>
///  </code>
/// </example>
[Serializable]
public struct SnowId : IComparable<SnowId>, IEquatable<SnowId>, IConvertible
{
    private static readonly long __random = CalculateRandomValue();
    private static int __staticIncrement = new Random().Next();

    private readonly int _a;
    private readonly int _b;
    private readonly int _c;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="SnowId" /> class</para>
    ///     <para xml:lang="zh">初始化 <see cref="SnowId" /> 类的新实例</para>
    /// </summary>
    /// <param name="bytes">
    ///     <para xml:lang="en">The bytes</para>
    ///     <para xml:lang="zh">字节数组</para>
    /// </param>
    public SnowId(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
        ArgumentExceptionExtensions.ThrowIf(() => bytes.Length != 12, "Byte array must be 12 bytes long", nameof(bytes));
        FromByteArray(bytes, 0, out _a, out _b, out _c);
    }

    internal SnowId(byte[] bytes, int index)
    {
        FromByteArray(bytes, index, out _a, out _b, out _c);
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="SnowId" /> class</para>
    ///     <para xml:lang="zh">初始化 <see cref="SnowId" /> 类的新实例</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The SnowId string</para>
    ///     <para xml:lang="zh"><see cref="SnowId" /> 字符串</para>
    /// </param>
    public SnowId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Convert.FromHexString(value);
        FromByteArray(bytes, 0, out _a, out _b, out _c);
    }

    private SnowId(int a, int b, int c)
    {
        _a = a;
        _b = b;
        _c = c;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets an instance of <see cref="SnowId" /> with an empty value</para>
    ///     <para xml:lang="zh">获取值为空的 <see cref="SnowId" /> 实例</para>
    /// </summary>
    public static SnowId Empty => default;

    /// <summary>
    ///     <para xml:lang="en">Gets the timestamp</para>
    ///     <para xml:lang="zh">获取时间戳</para>
    /// </summary>
    public readonly int Timestamp => _a;

    /// <summary>
    ///     <para xml:lang="en">Gets the creation time (derived from the timestamp)</para>
    ///     <para xml:lang="zh">获取创建时间(从时间戳派生)</para>
    /// </summary>
    public readonly DateTime CreationTime => DateTime.UnixEpoch.AddSeconds((uint)Timestamp);

    /// <inheritdoc cref="IComparable" />
    public static bool operator <(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) < 0;

    /// <inheritdoc cref="IComparable" />
    public static bool operator <=(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) <= 0;

    /// <inheritdoc cref="IComparable" />
    public static bool operator ==(SnowId lhs, SnowId rhs) => lhs.Equals(rhs);

    /// <inheritdoc cref="IComparable" />
    public static bool operator !=(SnowId lhs, SnowId rhs) => !(lhs == rhs);

    /// <inheritdoc cref="IComparable" />
    public static bool operator >=(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) >= 0;

    /// <inheritdoc cref="IComparable" />
    public static bool operator >(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) > 0;

    /// <summary>
    ///     <para xml:lang="en">Generates a new <see cref="SnowId" /> with a unique value</para>
    ///     <para xml:lang="zh">生成具有唯一值的新 <see cref="SnowId" /></para>
    /// </summary>
    public static SnowId GenerateNewId() => GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));

    /// <summary>
    ///     <para xml:lang="en">Generates a new <see cref="SnowId" /> with a unique value (timestamp component based on the given date and time)</para>
    ///     <para xml:lang="zh">生成具有唯一值的新 <see cref="SnowId" /> (时间戳组件基于给定的日期时间)</para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp (expressed as a date and time)</para>
    ///     <para xml:lang="zh">时间戳 (表示为日期时间)</para>
    /// </param>
    public static SnowId GenerateNewId(DateTime timestamp) => GenerateNewId(GetTimestampFromDateTime(timestamp));

    /// <summary>
    ///     <para xml:lang="en">Generates a new <see cref="SnowId" /> with a unique value (with the given timestamp)</para>
    ///     <para xml:lang="zh">生成具有唯一值(具有给定时间戳)的新 <see cref="SnowId" /></para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp</para>
    ///     <para xml:lang="zh">时间戳</para>
    /// </param>
    public static SnowId GenerateNewId(int timestamp)
    {
        var increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
        return Create(timestamp, __random, increment);
    }

    /// <summary>
    ///     <para xml:lang="en">Parses a string and creates a new <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">分析字符串并创建新的 <see cref="SnowId" /></para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The string value</para>
    ///     <para xml:lang="zh">字符串值</para>
    /// </param>
    public static SnowId Parse(string s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s, nameof(s));
        if (TryParse(s, out var snowId))
        {
            return snowId;
        }
        var message = $"'{s}' is not a valid 24 digit hex string.";
        throw new FormatException(message);
    }

    /// <summary>
    ///     <para xml:lang="en">Attempts to parse a string and create a new <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">尝试分析字符串并创建新的 <see cref="SnowId" /></para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The string value</para>
    ///     <para xml:lang="zh">字符串值</para>
    /// </param>
    /// <param name="snowId">
    ///     <para xml:lang="en">A new <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">一个新的 <see cref="SnowId" /></para>
    /// </param>
    public static bool TryParse(string s, out SnowId snowId)
    {
        if (s is { Length: 24 })
        {
            if (s.TryParseHex(out var bytes))
            {
                snowId = new(bytes!);
                return true;
            }
        }
        snowId = default;
        return false;
    }

    private static long CalculateRandomValue()
    {
        var high = Random.StrictNext();
        var low = Random.StrictNext();
        var combined = (long)(((ulong)(uint)high << 32) | (uint)low);
        return combined & 0xffffffffff; // low order 5 bytes
    }

    private static SnowId Create(int timestamp, long random, int increment)
    {
        if (random is < 0 or > 0xffffffffff)
        {
            throw new ArgumentOutOfRangeException(nameof(random), "The random value must be between 0 and 1099511627775 (it must fit in 5 bytes).");
        }
        if (increment is < 0 or > 0xffffff)
        {
            throw new ArgumentOutOfRangeException(nameof(increment), "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
        }
        var b = (int)(random >> 8);              // first 4 bytes of random
        var c = (int)(random << 24) | increment; // 5th byte of random and 3 byte increment
        return new(timestamp, b, c);
    }

    private static int GetTimestampFromDateTime(DateTime timestamp)
    {
        var secondsSinceEpoch = (long)Math.Floor((timestamp.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds);
        return secondsSinceEpoch is < uint.MinValue or > uint.MaxValue ? throw new ArgumentOutOfRangeException(nameof(timestamp)) : (int)(uint)secondsSinceEpoch;
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static void FromByteArray(byte[] bytes, int offset, out int a, out int b, out int c)
    {
        a = (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        b = (bytes[offset + 4] << 24) | (bytes[offset + 5] << 16) | (bytes[offset + 6] << 8) | bytes[offset + 7];
        c = (bytes[offset + 8] << 24) | (bytes[offset + 9] << 16) | (bytes[offset + 10] << 8) | bytes[offset + 11];
    }

    // private static char ToHexChar(int value) => Convert.ToChar(value < 10 ? value + 48 : value + 55); // 全大写字符
    private static char ToHexChar(int value) => Convert.ToChar(value < 10 ? value + 48 : value + 87); // 全小写字符

    /// <summary>
    ///     <para xml:lang="en">Compares this <see cref="SnowId" /> to another <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">将此 <see cref="SnowId" /> 与另一个 <see cref="SnowId" /> 进行比较</para>
    /// </summary>
    /// <param name="other">
    ///     <para xml:lang="en">The other <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">另一个 <see cref="SnowId" /></para>
    /// </param>
    public readonly int CompareTo(SnowId other)
    {
        var result = ((uint)_a).CompareTo((uint)other._a);
        if (result != 0)
        {
            return result;
        }
        result = ((uint)_b).CompareTo((uint)other._b);
        return result != 0 ? result : ((uint)_c).CompareTo((uint)other._c);
    }

    /// <summary>
    ///     <para xml:lang="en">Compares this <see cref="SnowId" /> to another <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">将此 <see cref="SnowId" /> 与另一个 <see cref="SnowId" /> 进行比较</para>
    /// </summary>
    /// <param name="rhs">
    ///     <para xml:lang="en">The other <see cref="SnowId" /></para>
    ///     <para xml:lang="zh">另一个 <see cref="SnowId" /></para>
    /// </param>
    public readonly bool Equals(SnowId rhs) =>
        _a == rhs._a &&
        _b == rhs._b &&
        _c == rhs._c;

    /// <summary>
    ///     <para xml:lang="en">Compares this <see cref="SnowId" /> to another object</para>
    ///     <para xml:lang="zh">将此 <see cref="SnowId" /> 与另一个对象进行比较</para>
    /// </summary>
    /// <param name="obj">
    ///     <para xml:lang="en">The other object</para>
    ///     <para xml:lang="zh">另一个对象</para>
    /// </param>
    public readonly override bool Equals(object? obj) => obj is SnowId id && Equals(id);

    /// <summary>
    ///     <para xml:lang="en">Gets the hash code</para>
    ///     <para xml:lang="zh">获取哈希代码</para>
    /// </summary>
    public readonly override int GetHashCode()
    {
        var hash = 17;
        hash = (37 * hash) + _a.GetHashCode();
        hash = (37 * hash) + _b.GetHashCode();
        hash = (37 * hash) + _c.GetHashCode();
        return hash;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the <see cref="SnowId" /> to a byte array</para>
    ///     <para xml:lang="zh">将 <see cref="SnowId" /> 转换为字节数组</para>
    /// </summary>
    public readonly byte[] ToByteArray()
    {
        var bytes = new byte[12];
        ToByteArray(bytes, 0);
        return bytes;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the <see cref="SnowId" /> to a byte array</para>
    ///     <para xml:lang="zh">将 <see cref="SnowId" /> 转换为字节数组</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The destination array</para>
    ///     <para xml:lang="zh">目标数组</para>
    /// </param>
    /// <param name="offset">
    ///     <para xml:lang="en">The offset</para>
    ///     <para xml:lang="zh">偏移量</para>
    /// </param>
    public readonly void ToByteArray(byte[] destination, int offset)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentExceptionExtensions.ThrowIf(() => offset + 12 > destination.Length, "Not enough room in destination buffer.", nameof(offset));
        //if (offset + 12 > destination.Length)
        //{
        //    throw new ArgumentException("Not enough room in destination buffer.", nameof(offset));
        //}
        destination[offset + 0] = (byte)(_a >> 24);
        destination[offset + 1] = (byte)(_a >> 16);
        destination[offset + 2] = (byte)(_a >> 8);
        destination[offset + 3] = (byte)_a;
        destination[offset + 4] = (byte)(_b >> 24);
        destination[offset + 5] = (byte)(_b >> 16);
        destination[offset + 6] = (byte)(_b >> 8);
        destination[offset + 7] = (byte)_b;
        destination[offset + 8] = (byte)(_c >> 24);
        destination[offset + 9] = (byte)(_c >> 16);
        destination[offset + 10] = (byte)(_c >> 8);
        destination[offset + 11] = (byte)_c;
    }

    /// <summary>
    ///     <para xml:lang="en">Returns a string representation of the value</para>
    ///     <para xml:lang="zh">返回值的字符串表示形式</para>
    /// </summary>
    public readonly override string ToString()
    {
        var c = new char[24];
        c[0] = ToHexChar((_a >> 28) & 0x0f);
        c[1] = ToHexChar((_a >> 24) & 0x0f);
        c[2] = ToHexChar((_a >> 20) & 0x0f);
        c[3] = ToHexChar((_a >> 16) & 0x0f);
        c[4] = ToHexChar((_a >> 12) & 0x0f);
        c[5] = ToHexChar((_a >> 8) & 0x0f);
        c[6] = ToHexChar((_a >> 4) & 0x0f);
        c[7] = ToHexChar(_a & 0x0f);
        c[8] = ToHexChar((_b >> 28) & 0x0f);
        c[9] = ToHexChar((_b >> 24) & 0x0f);
        c[10] = ToHexChar((_b >> 20) & 0x0f);
        c[11] = ToHexChar((_b >> 16) & 0x0f);
        c[12] = ToHexChar((_b >> 12) & 0x0f);
        c[13] = ToHexChar((_b >> 8) & 0x0f);
        c[14] = ToHexChar((_b >> 4) & 0x0f);
        c[15] = ToHexChar(_b & 0x0f);
        c[16] = ToHexChar((_c >> 28) & 0x0f);
        c[17] = ToHexChar((_c >> 24) & 0x0f);
        c[18] = ToHexChar((_c >> 20) & 0x0f);
        c[19] = ToHexChar((_c >> 16) & 0x0f);
        c[20] = ToHexChar((_c >> 12) & 0x0f);
        c[21] = ToHexChar((_c >> 8) & 0x0f);
        c[22] = ToHexChar((_c >> 4) & 0x0f);
        c[23] = ToHexChar(_c & 0x0f);
        return new(c);
    }

    readonly TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

    readonly bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();

    readonly byte IConvertible.ToByte(IFormatProvider? provider) => throw new InvalidCastException();

    readonly char IConvertible.ToChar(IFormatProvider? provider) => throw new InvalidCastException();

    readonly DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();

    readonly decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();

    readonly double IConvertible.ToDouble(IFormatProvider? provider) => throw new InvalidCastException();

    readonly short IConvertible.ToInt16(IFormatProvider? provider) => throw new InvalidCastException();

    readonly int IConvertible.ToInt32(IFormatProvider? provider) => throw new InvalidCastException();

    readonly long IConvertible.ToInt64(IFormatProvider? provider) => throw new InvalidCastException();

    readonly sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new InvalidCastException();

    readonly float IConvertible.ToSingle(IFormatProvider? provider) => throw new InvalidCastException();

    readonly string IConvertible.ToString(IFormatProvider? provider) => ToString();

    readonly object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (Type.GetTypeCode(conversionType))
        {
            case TypeCode.String:
                return ((IConvertible)this).ToString(provider);
            case TypeCode.Object:
                if (conversionType == typeof(object) || conversionType == typeof(SnowId))
                {
                    return this;
                }
                break;
        }
        throw new InvalidCastException();
    }

    readonly ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();

    readonly uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();

    readonly ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
}