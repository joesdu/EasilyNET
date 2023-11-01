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
using System.Runtime.CompilerServices;
using System.Security;
#pragma warning disable IDE0048

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.BaseType;

/// <summary>
/// <see cref="SnowId" /> 算法兼容MongoDB的 <see langword="ObjectId" /> ,因此他们可以互相强制转换
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
    /// 初始化 <see cref="SnowId" /> 类的新实例
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    public SnowId(byte[] bytes)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
#else
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
#endif
        ArgumentExceptionExtensions.ThrowIf(() => bytes.Length != 12, "Byte array must be 12 bytes long", nameof(bytes));
        //if (bytes.Length != 12)
        //{
        //    throw new ArgumentException("Byte array must be 12 bytes long", nameof(bytes));
        //}
        FromByteArray(bytes, 0, out _a, out _b, out _c);
    }

    /// <summary>
    /// 初始化 <see cref="SnowId" /> 类的新实例
    /// </summary>
    /// <param name="bytes">字节数组.</param>
    /// <param name="index"><see cref="SnowId" /> 开始的字节数组的索引</param>
    internal SnowId(byte[] bytes, int index)
    {
        FromByteArray(bytes, index, out _a, out _b, out _c);
    }

    /// <summary>
    /// 初始化 <see cref="SnowId" /> 类的新实例
    /// </summary>
    /// <param name="value"><see cref="SnowId" /> 字符串</param>
    public SnowId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = value.ParseHex();
        FromByteArray(bytes, 0, out _a, out _b, out _c);
    }

    private SnowId(int a, int b, int c)
    {
        _a = a;
        _b = b;
        _c = c;
    }

    // public static properties
    /// <summary>
    /// 获取值为空的 <see cref="SnowId" /> 实例
    /// </summary>
    public static SnowId Empty => default;

    // public properties
    /// <summary>
    /// 获取时间戳
    /// </summary>
    public readonly int Timestamp => _a;

    /// <summary>
    /// 获取创建时间(从时间戳派生)
    /// </summary>
    public readonly DateTime CreationTime => DateTimeStampExtension.UnixEpoch.AddSeconds((uint)Timestamp);

    // public operators
    /// <summary>
    /// 比较两个 <see cref="SnowId" />
    /// </summary>
    /// <param name="lhs">第一个 <see cref="SnowId" /></param>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果第一个 SnowId 小于第二个 SnowId,则为 True</returns>
    public static bool operator <(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) < 0;

    /// <summary>
    /// 比较两个 <see cref="SnowId" />
    /// </summary>
    /// <param name="lhs">第一个 <see cref="SnowId" /></param>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果第一个 <see cref="SnowId" /> 小于或等于第二个 <see cref="SnowId" />,则为 <see langword="true" /></returns>
    public static bool operator <=(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) <= 0;

    /// <summary>
    /// 比较两个 <see cref="SnowId" />
    /// </summary>
    /// <param name="lhs">第一个 <see cref="SnowId" /></param>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果两个 <see cref="SnowId" /> 相等,则为 True</returns>
    public static bool operator ==(SnowId lhs, SnowId rhs) => lhs.Equals(rhs);

    /// <summary>
    /// 比较两个 <see cref="SnowId" />
    /// </summary>
    /// <param name="lhs">第一个 <see cref="SnowId" /></param>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果两个 <see cref="SnowId" /> 不相等,则为 True</returns>
    public static bool operator !=(SnowId lhs, SnowId rhs) => !(lhs == rhs);

    /// <summary>
    /// 比较两个 <see cref="SnowId" />
    /// </summary>
    /// <param name="lhs">第一个 <see cref="SnowId" /></param>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果第一个 <see cref="SnowId" /> 大于或等于第二个 <see cref="SnowId" />,则为 True</returns>
    public static bool operator >=(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) >= 0;

    /// <summary>
    /// 比较两个 <see cref="SnowId" />
    /// </summary>
    /// <param name="lhs">第一个 <see cref="SnowId" /></param>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果第一个 <see cref="SnowId" /> 大于第二个 <see cref="SnowId" />,则为 True</returns>
    public static bool operator >(SnowId lhs, SnowId rhs) => lhs.CompareTo(rhs) > 0;

    // public static methods
    /// <summary>
    /// 生成具有唯一值的新 <see cref="SnowId" />
    /// </summary>
    /// <returns>一个 <see cref="SnowId" /></returns>
    public static SnowId GenerateNewId() => GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));

    /// <summary>
    /// 生成具有唯一值的新 <see cref="SnowId" /> (时间戳组件基于给定的日期时间)
    /// </summary>
    /// <param name="timestamp">时间戳 (表示为日期时间)</param>
    /// <returns>一个 <see cref="SnowId" /></returns>
    public static SnowId GenerateNewId(DateTime timestamp) => GenerateNewId(GetTimestampFromDateTime(timestamp));

    /// <summary>
    /// 生成具有唯一值(具有给定时间戳)的新 <see cref="SnowId" />
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <returns>一个 <see cref="SnowId" /></returns>
    public static SnowId GenerateNewId(int timestamp)
    {
        var increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
        return Create(timestamp, __random, increment);
    }

    /// <summary>
    /// 分析字符串并创建新的 <see cref="SnowId" />
    /// </summary>
    /// <param name="s">字符串值</param>
    /// <returns>一个 <see cref="SnowId" /> 对象</returns>
    public static SnowId Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentNullException(nameof(s));
        }
        if (TryParse(s, out var snowId))
        {
            return snowId;
        }
        var message = $"'{s}' is not a valid 24 digit hex string.";
        throw new FormatException(message);
    }

    /// <summary>
    /// 尝试分析字符串并创建新的 <see cref="SnowId" />
    /// </summary>
    /// <param name="s">字符串值</param>
    /// <param name="snowId">一个新的 <see cref="SnowId" /></param>
    /// <returns>如果字符串已成功分析,则为 <see langword="true" /></returns>
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
        var seed = (int)DateTime.UtcNow.Ticks ^ GetMachineHash() ^ GetPid();
        var random = new Random(seed);
        var high = random.Next();
        var low = random.Next();
        var combined = (long)((ulong)(uint)high << 32 | (uint)low);
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

    /// <summary>
    /// 获取当前进程 ID.此方法之所以存在,是因为 CAS 在调用堆栈上的操作方式,在执行方法之前检查权限.
    /// 因此,如果我们内联此调用,则调用方法将不会在引发异常之前执行,该异常要求在我们不一定控制的更高级别进行try/catch
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetCurrentProcessId() => Environment.ProcessId;

    private static int GetMachineHash()
    {
        // use instead of Dns.HostName so it will work offline
        var machineName = GetMachineName();
        return 0x00ffffff & machineName.GetHashCode(); // use first 3 bytes of hash
    }

    private static string GetMachineName() => Environment.MachineName;

    private static short GetPid()
    {
        try
        {
            return (short)GetCurrentProcessId(); // use low order two bytes only
        }
        catch (SecurityException)
        {
            return 0;
        }
    }

    private static int GetTimestampFromDateTime(DateTime timestamp)
    {
        var secondsSinceEpoch = (long)Math.Floor((timestamp.ToUniversalTime() - DateTimeStampExtension.UnixEpoch).TotalSeconds);
        return secondsSinceEpoch is < uint.MinValue or > uint.MaxValue ? throw new ArgumentOutOfRangeException(nameof(timestamp)) : (int)(uint)secondsSinceEpoch;
    }

    private static void FromByteArray(IReadOnlyList<byte> bytes, int offset, out int a, out int b, out int c)
    {
        a = bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
        b = bytes[offset + 4] << 24 | bytes[offset + 5] << 16 | bytes[offset + 6] << 8 | bytes[offset + 7];
        c = bytes[offset + 8] << 24 | bytes[offset + 9] << 16 | bytes[offset + 10] << 8 | bytes[offset + 11];
    }

    // public methods
    /// <summary>
    /// 将此 <see cref="SnowId" /> 与另一个 <see cref="SnowId" /> 进行比较
    /// </summary>
    /// <param name="other">另一个 <see cref="SnowId" /></param>
    /// <returns>一个 32 位有符号整数,指示此 <see cref="SnowId" /> 是小于,等于还是大于另一个</returns>
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
    /// 将此 <see cref="SnowId" /> 与另一个 <see cref="SnowId" /> 进行比较
    /// </summary>
    /// <param name="rhs">另一个 <see cref="SnowId" /></param>
    /// <returns>如果两个 <see cref="SnowId" /> 相等,则为 <see langword="true" /></returns>
    public readonly bool Equals(SnowId rhs) =>
        _a == rhs._a &&
        _b == rhs._b &&
        _c == rhs._c;

    /// <summary>
    /// 将此 <see cref="SnowId" /> 与另一个对象进行比较
    /// </summary>
    /// <param name="obj">另一个对象</param>
    /// <returns>如果另一个对象是 <see cref="SnowId" /> 并且等于此对象,则为 <see langword="true" /></returns>
    public readonly override bool Equals(object? obj) => obj is SnowId id && Equals(id);

    /// <summary>
    /// 获取哈希代码
    /// </summary>
    /// <returns>哈希代码</returns>
    public readonly override int GetHashCode()
    {
        var hash = 17;
        hash = 37 * hash + _a.GetHashCode();
        hash = 37 * hash + _b.GetHashCode();
        hash = 37 * hash + _c.GetHashCode();
        return hash;
    }

    /// <summary>
    /// 将 <see cref="SnowId" /> 转换为字节数组
    /// </summary>
    /// <returns>
    ///     <see langword="byte[]" />
    /// </returns>
    public readonly byte[] ToByteArray()
    {
        var bytes = new byte[12];
        ToByteArray(bytes, 0);
        return bytes;
    }

    /// <summary>
    /// 将 <see cref="SnowId" /> 转换为字节数组
    /// </summary>
    /// <param name="destination">目标数组</param>
    /// <param name="offset">偏移量</param>
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
    /// 返回值的字符串表示形式
    /// </summary>
    /// <returns>值的字符串表示形式</returns>
    public readonly override string ToString()
    {
        var c = new char[24];
        c[0] = (_a >> 28 & 0x0f).ToHexChar();
        c[1] = (_a >> 24 & 0x0f).ToHexChar();
        c[2] = (_a >> 20 & 0x0f).ToHexChar();
        c[3] = (_a >> 16 & 0x0f).ToHexChar();
        c[4] = (_a >> 12 & 0x0f).ToHexChar();
        c[5] = (_a >> 8 & 0x0f).ToHexChar();
        c[6] = (_a >> 4 & 0x0f).ToHexChar();
        c[7] = (_a & 0x0f).ToHexChar();
        c[8] = (_b >> 28 & 0x0f).ToHexChar();
        c[9] = (_b >> 24 & 0x0f).ToHexChar();
        c[10] = (_b >> 20 & 0x0f).ToHexChar();
        c[11] = (_b >> 16 & 0x0f).ToHexChar();
        c[12] = (_b >> 12 & 0x0f).ToHexChar();
        c[13] = (_b >> 8 & 0x0f).ToHexChar();
        c[14] = (_b >> 4 & 0x0f).ToHexChar();
        c[15] = (_b & 0x0f).ToHexChar();
        c[16] = (_c >> 28 & 0x0f).ToHexChar();
        c[17] = (_c >> 24 & 0x0f).ToHexChar();
        c[18] = (_c >> 20 & 0x0f).ToHexChar();
        c[19] = (_c >> 16 & 0x0f).ToHexChar();
        c[20] = (_c >> 12 & 0x0f).ToHexChar();
        c[21] = (_c >> 8 & 0x0f).ToHexChar();
        c[22] = (_c >> 4 & 0x0f).ToHexChar();
        c[23] = (_c & 0x0f).ToHexChar();
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
            case TypeCode.String: return ((IConvertible)this).ToString(provider);
            case TypeCode.Object:
                if (conversionType == typeof(object) || conversionType == typeof(SnowId)) return this;
                break;
        }
        throw new InvalidCastException();
    }

    readonly ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();

    readonly uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();

    readonly ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
}