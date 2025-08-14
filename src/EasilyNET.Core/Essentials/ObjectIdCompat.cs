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

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EasilyNET.Core.Misc;

#pragma warning disable IDE0048

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">ObjectIdCompat algorithm compatible with MongoDB's ObjectId, so they can be cast to each other</para>
///     <para xml:lang="zh">ObjectIdCompat 算法兼容MongoDB的 ObjectId, 因此他们可以互相强制转换</para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
/// var snow_id = ObjectIdCompat.GenerateNewId();
///   ]]>
///  </code>
/// </example>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ObjectIdCompat : IComparable<ObjectIdCompat>, IEquatable<ObjectIdCompat>, IConvertible
{
    private static readonly long __random = CalculateRandomValue();
    private static int __staticIncrement = Random.Shared.Next();

    private readonly int _b;
    private readonly int _c;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bytes"></param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObjectIdCompat(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 12)
        {
            throw new ArgumentException("Byte array must be 12 bytes long", nameof(bytes));
        }
        Timestamp = BinaryPrimitives.ReadInt32BigEndian(bytes);
        _b = BinaryPrimitives.ReadInt32BigEndian(bytes[4..]);
        _c = BinaryPrimitives.ReadInt32BigEndian(bytes[8..]);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bytes"></param>
    public ObjectIdCompat(byte[] bytes) : this((ReadOnlySpan<byte>)bytes) { }

    internal ObjectIdCompat(byte[] bytes, int index) : this(bytes.AsSpan(index, 12)) { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="value"></param>
    public ObjectIdCompat(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!TryParse(value, out this))
        {
            throw new FormatException($"'{value}' is not a valid 24 digit hex string.");
        }
    }

    private ObjectIdCompat(int a, int b, int c)
    {
        Timestamp = a;
        _b = b;
        _c = c;
    }

    /// <summary>
    /// Empty
    /// </summary>
    public static ObjectIdCompat Empty => default;

    /// <summary>
    /// Timestamp
    /// </summary>
    public int Timestamp { get; }

    /// <summary>
    /// CreationTime
    /// </summary>
    public DateTime CreationTime => DateTime.UnixEpoch.AddSeconds((uint)Timestamp);

    /// <inheritdoc cref="IComparable" />
    public static bool operator <(ObjectIdCompat lhs, ObjectIdCompat rhs) => lhs.CompareTo(rhs) < 0;

    /// <inheritdoc cref="IComparable" />
    public static bool operator <=(ObjectIdCompat lhs, ObjectIdCompat rhs) => lhs.CompareTo(rhs) <= 0;

    /// <inheritdoc cref="IComparable" />
    public static bool operator ==(ObjectIdCompat lhs, ObjectIdCompat rhs) => lhs.Equals(rhs);

    /// <inheritdoc cref="IComparable" />
    public static bool operator !=(ObjectIdCompat lhs, ObjectIdCompat rhs) => !(lhs == rhs);

    /// <inheritdoc cref="IComparable" />
    public static bool operator >=(ObjectIdCompat lhs, ObjectIdCompat rhs) => lhs.CompareTo(rhs) >= 0;

    /// <inheritdoc cref="IComparable" />
    public static bool operator >(ObjectIdCompat lhs, ObjectIdCompat rhs) => lhs.CompareTo(rhs) > 0;

    /// <summary>
    ///     <para xml:lang="en">Generates a new <see cref="ObjectIdCompat" /> with a unique value</para>
    ///     <para xml:lang="zh">生成具有唯一值的新 <see cref="ObjectIdCompat" /></para>
    /// </summary>
    public static ObjectIdCompat GenerateNewId() => GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));

    /// <summary>
    ///     <para xml:lang="en">Generates a new <see cref="ObjectIdCompat" /> with a unique value (timestamp component based on the given date and time)</para>
    ///     <para xml:lang="zh">生成具有唯一值的新 <see cref="ObjectIdCompat" /> (时间戳组件基于给定的日期时间)</para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp (expressed as a date and time)</para>
    ///     <para xml:lang="zh">时间戳 (表示为日期时间)</para>
    /// </param>
    public static ObjectIdCompat GenerateNewId(DateTime timestamp) => GenerateNewId(GetTimestampFromDateTime(timestamp));

    /// <summary>
    ///     <para xml:lang="en">Generates a new <see cref="ObjectIdCompat" /> with a unique value (with the given timestamp)</para>
    ///     <para xml:lang="zh">生成具有唯一值(具有给定时间戳)的新 <see cref="ObjectIdCompat" /></para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp</para>
    ///     <para xml:lang="zh">时间戳</para>
    /// </param>
    public static ObjectIdCompat GenerateNewId(int timestamp)
    {
        var increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
        return Create(timestamp, __random, increment);
    }

    /// <summary>
    ///     <para xml:lang="en">Parses a string and creates a new <see cref="ObjectIdCompat" /></para>
    ///     <para xml:lang="zh">分析字符串并创建新的 <see cref="ObjectIdCompat" /></para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The string value</para>
    ///     <para xml:lang="zh">字符串值</para>
    /// </param>
    public static ObjectIdCompat Parse(string s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s, nameof(s));
        return TryParse(s, out var snowId) ? snowId : throw new FormatException($"'{s}' is not a valid 24 digit hex string.");
    }

    /// <summary>
    ///     <para xml:lang="en">Parses a string and creates a new <see cref="ObjectIdCompat" /></para>
    ///     <para xml:lang="zh">分析字符串并创建新的 <see cref="ObjectIdCompat" /></para>
    /// </summary>
    /// <param name="s">
    ///     <para xml:lang="en">The string value</para>
    ///     <para xml:lang="zh">字符串值</para>
    /// </param>
    /// <param name="snowId"></param>
    // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
    public static bool TryParse(string? s, out ObjectIdCompat snowId)
    {
        if (s?.Length is 24)
        {
            return TryParse(s.AsSpan(), out snowId);
        }
        snowId = default;
        return false;
    }

    /// <summary>
    /// TryParse
    /// </summary>
    /// <param name="s"></param>
    /// <param name="snowId"></param>
    /// <returns></returns>
    public static bool TryParse(ReadOnlySpan<char> s, out ObjectIdCompat snowId)
    {
        snowId = default;
        if (s.Length != 24)
        {
            return false;
        }
        Span<byte> bytes = stackalloc byte[12];
        for (var i = 0; i < 12; i++)
        {
            var c1 = s[i * 2];
            var c2 = s[(i * 2) + 1];
            var v1 = HexCharToVal(c1);
            var v2 = HexCharToVal(c2);
            if (v1 == -1 || v2 == -1)
            {
                return false;
            }
            bytes[i] = (byte)((v1 << 4) | v2);
        }
        snowId = new(bytes);
        return true;
    }

    private static int HexCharToVal(char c) =>
        c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => (c - 'a') + 10,
            >= 'A' and <= 'F' => (c - 'A') + 10,
            _                 => -1
        };

    private static long CalculateRandomValue()
    {
        var high = Random.StrictNext();
        var low = Random.StrictNext();
        var combined = (long)(((ulong)(uint)high << 32) | (uint)low);
        return combined & 0xffffffffff; // low order 5 bytes
    }

    private static ObjectIdCompat Create(int timestamp, long random, int increment)
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

    /// <summary>
    ///     <para xml:lang="en">Compares this <see cref="ObjectIdCompat" /> to another <see cref="ObjectIdCompat" /></para>
    ///     <para xml:lang="zh">将此 <see cref="ObjectIdCompat" /> 与另一个 <see cref="ObjectIdCompat" /> 进行比较</para>
    /// </summary>
    /// <param name="other">
    ///     <para xml:lang="en">The other <see cref="ObjectIdCompat" /></para>
    ///     <para xml:lang="zh">另一个 <see cref="ObjectIdCompat" /></para>
    /// </param>
    public int CompareTo(ObjectIdCompat other)
    {
        var result = ((uint)Timestamp).CompareTo((uint)other.Timestamp);
        if (result != 0)
        {
            return result;
        }
        result = ((uint)_b).CompareTo((uint)other._b);
        return result != 0 ? result : ((uint)_c).CompareTo((uint)other._c);
    }

    /// <summary>
    ///     <para xml:lang="en">Compares this <see cref="ObjectIdCompat" /> to another <see cref="ObjectIdCompat" /></para>
    ///     <para xml:lang="zh">将此 <see cref="ObjectIdCompat" /> 与另一个 <see cref="ObjectIdCompat" /> 进行比较</para>
    /// </summary>
    /// <param name="rhs">
    ///     <para xml:lang="en">The other <see cref="ObjectIdCompat" /></para>
    ///     <para xml:lang="zh">另一个 <see cref="ObjectIdCompat" /></para>
    /// </param>
    public bool Equals(ObjectIdCompat rhs) =>
        Timestamp == rhs.Timestamp &&
        _b == rhs._b &&
        _c == rhs._c;

    /// <summary>
    ///     <para xml:lang="en">Compares this <see cref="ObjectIdCompat" /> to another object</para>
    ///     <para xml:lang="zh">将此 <see cref="ObjectIdCompat" /> 与另一个对象进行比较</para>
    /// </summary>
    /// <param name="obj">
    ///     <para xml:lang="en">The other object</para>
    ///     <para xml:lang="zh">另一个对象</para>
    /// </param>
    public override bool Equals(object? obj) => obj is ObjectIdCompat id && Equals(id);

    /// <summary>
    ///     <para xml:lang="en">Gets the hash code</para>
    ///     <para xml:lang="zh">获取哈希代码</para>
    /// </summary>
    public override int GetHashCode()
    {
        var hash = 17;
        hash = (37 * hash) + Timestamp.GetHashCode();
        hash = (37 * hash) + _b.GetHashCode();
        hash = (37 * hash) + _c.GetHashCode();
        return hash;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the <see cref="ObjectIdCompat" /> to a byte array</para>
    ///     <para xml:lang="zh">将 <see cref="ObjectIdCompat" /> 转换为字节数组</para>
    /// </summary>
    public byte[] ToByteArray()
    {
        var bytes = new byte[12];
        ToByteArray(bytes, 0);
        return bytes;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the <see cref="ObjectIdCompat" /> to a byte array</para>
    ///     <para xml:lang="zh">将 <see cref="ObjectIdCompat" /> 转换为字节数组</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The destination array</para>
    ///     <para xml:lang="zh">目标数组</para>
    /// </param>
    /// <param name="offset">
    ///     <para xml:lang="en">The offset</para>
    ///     <para xml:lang="zh">偏移量</para>
    /// </param>
    public void ToByteArray(byte[] destination, int offset)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (offset + 12 > destination.Length)
        {
            throw new ArgumentException("Not enough room in destination buffer.", nameof(offset));
        }
        BinaryPrimitives.WriteInt32BigEndian(destination.AsSpan(offset, 4), Timestamp);
        BinaryPrimitives.WriteInt32BigEndian(destination.AsSpan(offset + 4, 4), _b);
        BinaryPrimitives.WriteInt32BigEndian(destination.AsSpan(offset + 8, 4), _c);
    }

    /// <summary>
    /// ToByteArray
    /// </summary>
    /// <param name="destination"></param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToByteArray(Span<byte> destination)
    {
        if (destination.Length < 12)
        {
            throw new ArgumentException("Not enough room in destination buffer.", nameof(destination));
        }
        BinaryPrimitives.WriteInt32BigEndian(destination, Timestamp);
        BinaryPrimitives.WriteInt32BigEndian(destination[4..], _b);
        BinaryPrimitives.WriteInt32BigEndian(destination[8..], _c);
    }

    /// <summary>
    ///     <para xml:lang="en">Returns a string representation of the value</para>
    ///     <para xml:lang="zh">返回值的字符串表示形式</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return string.Create(24, this, static (chars, id) =>
        {
            ReadOnlySpan<char> hexChars = "0123456789abcdef";
            Span<byte> bytes = stackalloc byte[12];
            id.ToByteArray(bytes);
            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                var i2 = i * 2;
                chars[i2] = hexChars[b >> 4];
                chars[i2 + 1] = hexChars[b & 0x0F];
            }
        });
    }

    TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

    bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();

    byte IConvertible.ToByte(IFormatProvider? provider) => throw new InvalidCastException();

    char IConvertible.ToChar(IFormatProvider? provider) => throw new InvalidCastException();

    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();

    decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();

    double IConvertible.ToDouble(IFormatProvider? provider) => throw new InvalidCastException();

    short IConvertible.ToInt16(IFormatProvider? provider) => throw new InvalidCastException();

    int IConvertible.ToInt32(IFormatProvider? provider) => throw new InvalidCastException();

    long IConvertible.ToInt64(IFormatProvider? provider) => throw new InvalidCastException();

    sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new InvalidCastException();

    float IConvertible.ToSingle(IFormatProvider? provider) => throw new InvalidCastException();

    string IConvertible.ToString(IFormatProvider? provider) => ToString();

    object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (Type.GetTypeCode(conversionType))
        {
            case TypeCode.String:
                return ((IConvertible)this).ToString(provider);
            case TypeCode.Object:
                if (conversionType == typeof(object) || conversionType == typeof(ObjectIdCompat))
                {
                    return this;
                }
                break;
        }
        throw new InvalidCastException();
    }

    ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();

    uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();

    ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
}