using System.Diagnostics.CodeAnalysis;
using System.Numerics;

#pragma warning disable IDE0079
#pragma warning disable IDE0048 // 为清楚起见，请添加括号
#pragma warning restore IDE0079

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Numerics;

/// <summary>
/// BigNumber类提供了大数的基本运算，包括加、减、乘、除、取模、幂运算等,
/// 用于将大十进制数和有理数与 System.Numerics.BigInteger 一起使用的库
/// </summary>
public sealed class BigNumber : IEquatable<BigNumber>, IComparable<BigNumber>
{
    /// <summary>
    /// 数值1
    /// </summary>
    public static readonly BigNumber One = new(1);

    /// <summary>
    /// 数值-1
    /// </summary>
    public static readonly BigNumber MinusOne = new(-1);

    /// <summary>
    /// 数值0
    /// </summary>
    public static readonly BigNumber Zero = new(0);

    /// <summary>
    /// 符号位
    /// </summary>
    private int Sign { get; set; }

    /// <summary>
    /// 整数部分
    /// </summary>
    private BigInteger Whole { get; set; }

    /// <summary>
    /// 分子
    /// </summary>
    private BigInteger Numerator { get; set; }

    /// <summary>
    /// 分母
    /// </summary>
    private BigInteger Denominator { get; set; }

    /// <summary>
    /// 基于当前值创建浅拷贝
    /// </summary>
    private BigNumber CloneRaw() =>
        new()
        {
            Whole = Whole,
            Numerator = Numerator,
            Denominator = Denominator,
            Sign = Sign
        };

    /// <summary>
    /// 以分数形式返回当前值 (分子/分母)，不破坏原始对象
    /// </summary>
    private (BigInteger numerator, BigInteger denominator) AsFraction()
    {
        var copy = CloneRaw();
        copy.Simplify();
        var numerator = (copy.Whole * copy.Denominator) + copy.Numerator;
        var denominator = copy.Denominator;
        return (numerator, denominator);
    }

    #region Constructors

    /// <summary>
    /// 设置BigNumber的值
    /// </summary>
    /// <param name="num"></param>
    public void SetBigNumber(BigNumber num)
    {
        Whole = num.Whole;
        Numerator = num.Numerator;
        Denominator = num.Denominator;
        Sign = num.Sign;
        Simplify();
    }

    #region BigInteger  (Int, Uint, long)

    // whole numerator/ denominator
    /// <summary>
    /// 从BigInteger创建BigNumber
    /// </summary>
    /// <param name="whole"></param>
    /// <returns></returns>
    public static BigNumber FromBigInteger(BigInteger whole)
    {
        var num = new BigNumber
        {
            Whole = whole - 1,
            Numerator = 1,
            Denominator = 1,
            Sign = whole.Sign
        };
        num.Simplify();
        return num;
    }

    /// <summary>
    /// 从分数创建BigNumber
    /// </summary>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static BigNumber FromBigInteger(BigInteger numerator, BigInteger denominator)
    {
        var num = new BigNumber
        {
            Whole = 0,
            Numerator = numerator,
            Denominator = denominator,
            Sign = numerator.Sign
        };
        if (num.Denominator == 0)
        {
            throw new ArgumentException("Denominator must not be 0");
        }
        num.Simplify();
        return num;
    }

    /// <summary>
    /// 从整数部分、分子和分母创建BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static BigNumber FromBigInteger(BigInteger whole, BigInteger numerator, BigInteger denominator)
    {
        var num = new BigNumber
        {
            Whole = whole,
            Numerator = numerator,
            Denominator = denominator
        };
        if (num.Denominator == 0)
        {
            throw new ArgumentException("Denominator must not be 0");
        }
        num.GetSign();
        num.Simplify();
        return num;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public BigNumber()
    {
        Whole = 0;
        Numerator = 0;
        Denominator = 1;
        Sign = 0;
    }

    /// <summary>
    /// 从整数初始化BigNumber
    /// </summary>
    /// <param name="whole"></param>
    public BigNumber(int whole)
    {
        Whole = whole;
        Numerator = 0;
        Denominator = 1;
        Sign = whole == 0
                   ? 0
                   : whole > 0
                       ? 1
                       : -1;
    }

    /// <summary>
    /// 从分数初始化BigNumber
    /// </summary>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(int numerator, int denominator)
        : this(numerator, (long)denominator) { }

    /// <summary>
    /// 从整数部分、分子和分母创建BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(int whole, int numerator, int denominator)
        : this(whole, numerator, (long)denominator) { }

    /// <summary>
    /// 从无符号整数初始化BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    public BigNumber(uint whole)
    {
        SetBigNumber(FromBigInteger(new(whole)));
    }

    /// <summary>
    /// 从分数初始化BigNumber
    /// </summary>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(uint numerator, uint denominator)
    {
        SetBigNumber(FromBigInteger(new(numerator), new(denominator)));
    }

    /// <summary>
    /// 从整数部分、分子和分母创建BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(uint whole, uint numerator, uint denominator)
    {
        SetBigNumber(FromBigInteger(new(whole), new(numerator), new(denominator)));
    }

    /// <summary>
    /// 从长整型初始化BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    public BigNumber(long whole)
    {
        Whole = whole;
        Numerator = 0;
        Denominator = 1;
        Sign = whole == 0
                   ? 0
                   : whole > 0
                       ? 1
                       : -1;
    }

    /// <summary>
    /// 从分数初始化BigNumber
    /// </summary>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException();
        }
        Whole = 0;
        Numerator = numerator;
        Denominator = denominator;
        Simplify();
    }

    /// <summary>
    /// 从整数部分、分子和分母创建BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(long whole, long numerator, long denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException();
        }
        Whole = whole;
        Numerator = numerator;
        Denominator = denominator;
        Simplify();
    }

    /// <summary>
    /// 从无符号长整型初始化BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    public BigNumber(ulong whole)
    {
        SetBigNumber(FromBigInteger(new(whole)));
    }

    /// <summary>
    /// 从分数初始化BigNumber
    /// </summary>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(ulong numerator, ulong denominator)
    {
        SetBigNumber(FromBigInteger(new(numerator), new(denominator)));
    }

    /// <summary>
    /// 从整数部分、分子和分母创建BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    public BigNumber(ulong whole, ulong numerator, ulong denominator)
    {
        SetBigNumber(FromBigInteger(new(whole), new(numerator), new(denominator)));
    }

    #endregion

    #region Float

    /// <summary>
    /// 从浮点数初始化BigNumber
    /// </summary>
    /// <param name="whole"></param>
    /// <exception cref="ArgumentException"></exception>
    public BigNumber(float whole) : this((decimal)whole) { }

    #endregion

    #region Double

    /// <summary>
    /// 从双精度浮点数初始化BigNumber
    /// </summary>
    /// <param name="whole"></param>
    /// <exception cref="ArgumentException"></exception>
    public BigNumber(double whole) : this((decimal)whole) { }

    #endregion

    #region Decimal

    /// <summary>
    /// 从decimal初始化BigNumber
    /// </summary>
    /// <param name="whole"></param>
    /// <exception cref="ArgumentException"></exception>
    public BigNumber(decimal whole)
    {
        if (whole == decimal.Truncate(whole))
        {
            Whole = (BigInteger)whole;
            Numerator = 0;
            Denominator = 1;
            Sign = whole == 0
                       ? 0
                       : whole > 0
                           ? 1
                           : -1;
        }
        else
        {
            var bits = decimal.GetBits(whole);
            var isNegative = (bits[3] & 0x80000000) != 0;
            var scale = (byte)((bits[3] >> 16) & 0x7F);
            var numVal = new BigInteger((uint)bits[0]) +
                         (new BigInteger((uint)bits[1]) << 32) +
                         (new BigInteger((uint)bits[2]) << 64);
            if (isNegative)
            {
                numVal = -numVal;
            }
            var denVal = BigInteger.Pow(10, scale);
            Whole = 0;
            Numerator = numVal;
            Denominator = denVal;
            Simplify();
        }
    }

    #endregion

    #region String

    /// <summary>
    /// 从字符串初始化BigNumber
    /// </summary>
    /// <param name="whole">整数</param>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    /// <exception cref="ArgumentException"></exception>
    public BigNumber(string whole, string numerator, string denominator)
    {
        Whole = BigInteger.Parse(whole);
        Numerator = BigInteger.Parse(numerator);
        Denominator = BigInteger.Parse(denominator);
        if (Denominator <= 0)
        {
            throw new ArgumentException("Numerator or denominator must bigger than 3");
        }
        Sign = GetSign();
        Simplify();
    }

    /// <summary>
    /// 从字符串初始化BigNumber
    /// </summary>
    /// <param name="number">字符串</param>
    /// <param name="split">分隔符</param>
    /// <exception cref="ArgumentException"></exception>
    public BigNumber(string number, char split = ',')
    {
        if (number.Contains(split))
        {
            if (number.Split(split).Length > 2)
            {
                throw new ArgumentException("number can contain only 1 split character");
            }
            var arr = number.Split(split);
            Whole = BigInteger.Parse(arr[0]);
            Numerator = BigInteger.Parse(arr[1]);
            Denominator = new(Math.Pow(10, arr[1].Length));
            Sign = GetSign();
            Simplify();
        }
        else
        {
            Whole = BigInteger.Parse(number);
            Numerator = 1;
            Denominator = 1;
            Sign = GetSign();
            Simplify();
        }
    }

    #endregion

    #endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #region Booleans

    #region Equal

    public static bool operator ==(BigNumber? a, BigNumber? b) => ReferenceEquals(a, b) || (a is not null && b is not null && a.CompareTo(b) == 0);

    public static bool operator ==(int a, BigNumber b) => Equal(new(a), b);
    public static bool operator ==(BigNumber a, int b) => Equal(a, new(b));
    public static bool operator ==(uint a, BigNumber b) => Equal(new(a), b);
    public static bool operator ==(BigNumber a, uint b) => Equal(a, new(b));
    public static bool operator ==(long a, BigNumber b) => Equal(new(a), b);
    public static bool operator ==(BigNumber a, long b) => Equal(a, new(b));
    public static bool operator ==(float a, BigNumber b) => Equal(new(a), b);
    public static bool operator ==(BigNumber a, float b) => Equal(a, new(b));
    public static bool operator ==(double a, BigNumber b) => Equal(new(a), b);
    public static bool operator ==(BigNumber a, double b) => Equal(a, new(b));
    public static bool operator ==(decimal a, BigNumber b) => Equal(new(a), b);
    public static bool operator ==(BigNumber a, decimal b) => Equal(a, new(b));

    #endregion

    #region Not Equal

    public static bool operator !=(BigNumber? a, BigNumber? b) => !(a == b);
    public static bool operator !=(int a, BigNumber b) => NotEqual(new(a), b);
    public static bool operator !=(BigNumber a, int b) => NotEqual(a, new(b));
    public static bool operator !=(uint a, BigNumber b) => NotEqual(new(a), b);
    public static bool operator !=(BigNumber a, uint b) => NotEqual(a, new(b));
    public static bool operator !=(long a, BigNumber b) => NotEqual(new(a), b);
    public static bool operator !=(BigNumber a, long b) => NotEqual(a, new(b));
    public static bool operator !=(float a, BigNumber b) => NotEqual(new(a), b);
    public static bool operator !=(BigNumber a, float b) => NotEqual(a, new(b));
    public static bool operator !=(double a, BigNumber b) => NotEqual(new(a), b);
    public static bool operator !=(BigNumber a, double b) => NotEqual(a, new(b));
    public static bool operator !=(decimal a, BigNumber b) => NotEqual(new(a), b);
    public static bool operator !=(BigNumber a, decimal b) => NotEqual(a, new(b));

    #endregion

    #region Smaller

    public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
    public static bool operator <(int a, BigNumber b) => Smaller(new(a), b);
    public static bool operator <(BigNumber a, int b) => Smaller(a, new(b));
    public static bool operator <(uint a, BigNumber b) => Smaller(new(a), b);
    public static bool operator <(BigNumber a, uint b) => Smaller(a, new(b));
    public static bool operator <(long a, BigNumber b) => Smaller(new(a), b);
    public static bool operator <(BigNumber a, long b) => Smaller(a, new(b));
    public static bool operator <(float a, BigNumber b) => Smaller(new(a), b);
    public static bool operator <(BigNumber a, float b) => Smaller(a, new(b));
    public static bool operator <(double a, BigNumber b) => Smaller(new(a), b);
    public static bool operator <(BigNumber a, double b) => Smaller(a, new(b));
    public static bool operator <(decimal a, BigNumber b) => Smaller(new(a), b);
    public static bool operator <(BigNumber a, decimal b) => Smaller(a, new(b));

    #endregion

    #region Smaller Or Equal

    public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
    public static bool operator <=(int a, BigNumber b) => SmallerOrEqual(new(a), b);
    public static bool operator <=(BigNumber a, int b) => SmallerOrEqual(a, new(b));
    public static bool operator <=(uint a, BigNumber b) => SmallerOrEqual(new(a), b);
    public static bool operator <=(BigNumber a, uint b) => SmallerOrEqual(a, new(b));
    public static bool operator <=(long a, BigNumber b) => SmallerOrEqual(new(a), b);
    public static bool operator <=(BigNumber a, long b) => SmallerOrEqual(a, new(b));
    public static bool operator <=(float a, BigNumber b) => SmallerOrEqual(new(a), b);
    public static bool operator <=(BigNumber a, float b) => SmallerOrEqual(a, new(b));
    public static bool operator <=(double a, BigNumber b) => SmallerOrEqual(new(a), b);
    public static bool operator <=(BigNumber a, double b) => SmallerOrEqual(a, new(b));
    public static bool operator <=(decimal a, BigNumber b) => SmallerOrEqual(new(a), b);
    public static bool operator <=(BigNumber a, decimal b) => SmallerOrEqual(a, new(b));

    #endregion

    #region Bigger

    public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
    public static bool operator >(int a, BigNumber b) => Bigger(new(a), b);
    public static bool operator >(BigNumber a, int b) => Bigger(a, new(b));
    public static bool operator >(uint a, BigNumber b) => Bigger(new(a), b);
    public static bool operator >(BigNumber a, uint b) => Bigger(a, new(b));
    public static bool operator >(long a, BigNumber b) => Bigger(new(a), b);
    public static bool operator >(BigNumber a, long b) => Bigger(a, new(b));
    public static bool operator >(float a, BigNumber b) => Bigger(new(a), b);
    public static bool operator >(BigNumber a, float b) => Bigger(a, new(b));
    public static bool operator >(double a, BigNumber b) => Bigger(new(a), b);
    public static bool operator >(BigNumber a, double b) => Bigger(a, new(b));
    public static bool operator >(decimal a, BigNumber b) => Bigger(new(a), b);
    public static bool operator >(BigNumber a, decimal b) => Bigger(a, new(b));

    #endregion

    #region Bigger Or Equal

    public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
    public static bool operator >=(int a, BigNumber b) => BiggerOrEqual(new(a), b);
    public static bool operator >=(BigNumber a, int b) => BiggerOrEqual(a, new(b));
    public static bool operator >=(uint a, BigNumber b) => BiggerOrEqual(new(a), b);
    public static bool operator >=(BigNumber a, uint b) => BiggerOrEqual(a, new(b));
    public static bool operator >=(long a, BigNumber b) => BiggerOrEqual(new(a), b);
    public static bool operator >=(BigNumber a, long b) => BiggerOrEqual(a, new(b));
    public static bool operator >=(float a, BigNumber b) => BiggerOrEqual(new(a), b);
    public static bool operator >=(BigNumber a, float b) => BiggerOrEqual(a, new(b));
    public static bool operator >=(double a, BigNumber b) => BiggerOrEqual(new(a), b);
    public static bool operator >=(BigNumber a, double b) => BiggerOrEqual(a, new(b));
    public static bool operator >=(decimal a, BigNumber b) => BiggerOrEqual(new(a), b);
    public static bool operator >=(BigNumber a, decimal b) => BiggerOrEqual(a, new(b));

    #endregion

    #endregion

    #region Operations

    #region Addition

    public static BigNumber operator +(BigNumber a) => a;
    public static BigNumber operator ++(BigNumber a) => Add(a, new(1));

    public static BigNumber operator +(BigNumber a, BigNumber b)
    {
        var numA = (a.Whole * a.Denominator) + a.Numerator;
        var denA = a.Denominator;
        var numB = (b.Whole * b.Denominator) + b.Numerator;
        var denB = b.Denominator;
        var resultNumerator = (numA * denB) + (numB * denA);
        var resultDenominator = denA * denB;
        return FromBigInteger(resultNumerator, resultDenominator);
    }

    public static BigNumber operator +(int a, BigNumber b) => Add(new(a), b);
    public static BigNumber operator +(BigNumber a, int b) => Add(a, new(b));
    public static BigNumber operator +(uint a, BigNumber b) => Add(new(a), b);
    public static BigNumber operator +(BigNumber a, uint b) => Add(a, new(b));
    public static BigNumber operator +(long a, BigNumber b) => Add(new(a), b);
    public static BigNumber operator +(BigNumber a, long b) => Add(a, new(b));
    public static BigNumber operator +(float a, BigNumber b) => Add(new(a), b);
    public static BigNumber operator +(BigNumber a, float b) => Add(a, new(b));
    public static BigNumber operator +(double a, BigNumber b) => Add(new(a), b);
    public static BigNumber operator +(BigNumber a, double b) => Add(a, new(b));
    public static BigNumber operator +(decimal a, BigNumber b) => Add(new(a), b);
    public static BigNumber operator +(BigNumber a, decimal b) => Add(a, new(b));

    #endregion

    #region Subtraction

    public static BigNumber operator -(BigNumber a) => FromBigInteger(-a.Whole, -a.Numerator, a.Denominator);
    public static BigNumber operator --(BigNumber a) => Subtract(a, new(1));
    public static BigNumber operator -(BigNumber a, BigNumber b) => a + -b;
    public static BigNumber operator -(int a, BigNumber b) => Subtract(new(a), b);
    public static BigNumber operator -(BigNumber a, int b) => Subtract(a, new(b));
    public static BigNumber operator -(uint a, BigNumber b) => Subtract(new(a), b);
    public static BigNumber operator -(BigNumber a, uint b) => Subtract(a, new(b));
    public static BigNumber operator -(long a, BigNumber b) => Subtract(new(a), b);
    public static BigNumber operator -(BigNumber a, long b) => Subtract(a, new(b));
    public static BigNumber operator -(float a, BigNumber b) => Subtract(new(a), b);
    public static BigNumber operator -(BigNumber a, float b) => Subtract(a, new(b));
    public static BigNumber operator -(double a, BigNumber b) => Subtract(new(a), b);
    public static BigNumber operator -(BigNumber a, double b) => Subtract(a, new(b));
    public static BigNumber operator -(decimal a, BigNumber b) => Subtract(new(a), b);
    public static BigNumber operator -(BigNumber a, decimal b) => Subtract(a, new(b));

    #endregion

    #region Multiplycaiton

    public static BigNumber operator *(BigNumber a, BigNumber b)
    {
        var numA = (a.Whole * a.Denominator) + a.Numerator;
        var denA = a.Denominator;
        var numB = (b.Whole * b.Denominator) + b.Numerator;
        var denB = b.Denominator;
        return FromBigInteger(numA * numB, denA * denB);
    }

    public static BigNumber operator *(int a, BigNumber b) => Multiply(new(a), b);
    public static BigNumber operator *(BigNumber a, int b) => Multiply(a, new(b));
    public static BigNumber operator *(uint a, BigNumber b) => Multiply(new(a), b);
    public static BigNumber operator *(BigNumber a, uint b) => Multiply(a, new(b));
    public static BigNumber operator *(long a, BigNumber b) => Multiply(new(a), b);
    public static BigNumber operator *(BigNumber a, long b) => Multiply(a, new(b));
    public static BigNumber operator *(float a, BigNumber b) => Multiply(new(a), b);
    public static BigNumber operator *(BigNumber a, float b) => Multiply(a, new(b));
    public static BigNumber operator *(double a, BigNumber b) => Multiply(new(a), b);
    public static BigNumber operator *(BigNumber a, double b) => Multiply(a, new(b));
    public static BigNumber operator *(decimal a, BigNumber b) => Multiply(new(a), b);
    public static BigNumber operator *(BigNumber a, decimal b) => Multiply(a, new(b));

    #endregion

    #region Divison

    public static BigNumber operator /(BigNumber a, BigNumber b)
    {
        var numA = (a.Whole * a.Denominator) + a.Numerator;
        var denA = a.Denominator;
        var numB = (b.Whole * b.Denominator) + b.Numerator;
        var denB = b.Denominator;
        return numB == 0 ? throw new DivideByZeroException() : FromBigInteger(numA * denB, denA * numB);
    }

    public static BigNumber operator /(int a, BigNumber b) => Divide(new(a), b);
    public static BigNumber operator /(BigNumber a, int b) => Divide(a, new(b));
    public static BigNumber operator /(uint a, BigNumber b) => Divide(new(a), b);
    public static BigNumber operator /(BigNumber a, uint b) => Divide(a, new(b));
    public static BigNumber operator /(long a, BigNumber b) => Divide(new(a), b);
    public static BigNumber operator /(BigNumber a, long b) => Divide(a, new(b));
    public static BigNumber operator /(float a, BigNumber b) => Divide(new(a), b);
    public static BigNumber operator /(BigNumber a, float b) => Divide(a, new(b));
    public static BigNumber operator /(double a, BigNumber b) => Divide(new(a), b);
    public static BigNumber operator /(BigNumber a, double b) => Divide(a, new(b));
    public static BigNumber operator /(decimal a, BigNumber b) => Divide(new(a), b);
    public static BigNumber operator /(BigNumber a, decimal b) => Divide(a, new(b));

    #endregion

    #region Mod

    public static BigNumber operator %(BigNumber a, BigNumber b)
    {
        var (numA, denA) = a.AsFraction();
        var (numB, denB) = b.AsFraction();
        if (numB == 0)
        {
            throw new DivideByZeroException();
        }
        var quotient = BigInteger.Divide(numA * denB, denA * numB);
        return FromBigInteger(numA, denA) - (FromBigInteger(quotient) * b);
    }

    public static BigNumber operator %(int a, BigNumber b) => Mod(new(a), b);
    public static BigNumber operator %(BigNumber a, int b) => Mod(a, new(b));
    public static BigNumber operator %(uint a, BigNumber b) => Mod(new(a), b);
    public static BigNumber operator %(BigNumber a, uint b) => Mod(a, new(b));
    public static BigNumber operator %(long a, BigNumber b) => Mod(new(a), b);
    public static BigNumber operator %(BigNumber a, long b) => Mod(a, new(b));
    public static BigNumber operator %(float a, BigNumber b) => Mod(new(a), b);
    public static BigNumber operator %(BigNumber a, float b) => Mod(a, new(b));
    public static BigNumber operator %(double a, BigNumber b) => Mod(new(a), b);
    public static BigNumber operator %(BigNumber a, double b) => Mod(a, new(b));
    public static BigNumber operator %(decimal a, BigNumber b) => Mod(new(a), b);
    public static BigNumber operator %(BigNumber a, decimal b) => Mod(a, new(b));

    #endregion

    #region Bitwise

    public static BigNumber operator <<(BigNumber a, int b)
    {
        var (num, den) = a.AsFraction();
        // 分子左移，分母不变
        return FromBigInteger(num << b, den);
    }

    public static BigNumber operator >> (BigNumber a, int b)
    {
        var (num, den) = a.AsFraction();
        // 分子右移，分母不变
        return FromBigInteger(num >> b, den);
    }

    public static BigNumber operator ^(BigNumber a, BigNumber b)
    {
        var (numA, denA) = a.AsFraction();
        var (numB, denB) = b.AsFraction();
        // 通分
        var commonDen = BigInteger.Multiply(denA, denB);
        var aNum = numA * (commonDen / denA);
        var bNum = numB * (commonDen / denB);
        // 分子异或，分母不变
        var resultNum = aNum ^ bNum;
        return FromBigInteger(resultNum, commonDen);
    }

    #endregion

    #endregion

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    #region Funcs

    #region Operations

    /// <summary>
    /// 加法运算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigNumber Add(BigNumber a, BigNumber b)
    {
        var (numA, denA) = a.AsFraction();
        var (numB, denB) = b.AsFraction();
        var numerator = (numA * denB) + (numB * denA);
        var denominator = denA * denB;
        return FromBigInteger(numerator, denominator);
    }

    /// <summary>
    /// 加法运算
    /// </summary>
    /// <param name="other"></param>
    public void Add(BigNumber other) => SetBigNumber(Add(this, other));

    /// <summary>
    /// 减法运算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigNumber Subtract(BigNumber a, BigNumber b) => Add(a, b * -1);

    /// <summary>
    /// 减法运算
    /// </summary>
    /// <param name="other"></param>
    public void Subtract(BigNumber other) => SetBigNumber(Subtract(this, other));

    /// <summary>
    /// 乘法运算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigNumber Multiply(BigNumber a, BigNumber b)
    {
        var (numA, denA) = a.AsFraction();
        var (numB, denB) = b.AsFraction();
        return FromBigInteger(numA * numB, denA * denB);
    }

    /// <summary>
    /// 乘法运算
    /// </summary>
    /// <param name="other"></param>
    public void Multiply(BigNumber other) => SetBigNumber(Multiply(this, other));

    /// <summary>
    /// 除法运算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigNumber Divide(BigNumber a, BigNumber b)
    {
        var (numA, denA) = a.AsFraction();
        var (numB, denB) = b.AsFraction();
        return numB == 0 ? throw new DivideByZeroException() : FromBigInteger(numA * denB, denA * numB);
    }

    /// <summary>
    /// 除法运算
    /// </summary>
    /// <param name="other"></param>
    public void Divide(BigNumber other) => SetBigNumber(Divide(this, other));

    /// <summary>
    /// 取模运算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigNumber Mod(BigNumber a, BigNumber b)
    {
        var (numA, denA) = a.AsFraction();
        var (numB, denB) = b.AsFraction();
        if (numB == 0)
        {
            throw new DivideByZeroException();
        }
        var quotient = BigInteger.Divide(numA * denB, denA * numB);
        return FromBigInteger(numA, denA) - (FromBigInteger(quotient) * b);
    }

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigNumber Pow(BigNumber a, BigInteger b)
    {
        if (b < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(b), "Exponent must be non-negative.");
        }
        var (num, den) = a.AsFraction();
        var result = One;
        var baseVal = FromBigInteger(num, den);
        var exp = b;
        while (exp > 0)
        {
            if ((exp & 1) == 1)
            {
                result *= baseVal;
            }
            baseVal *= baseVal;
            exp >>= 1;
        }
        return result;
    }

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="a">底数</param>
    /// <param name="b">指数</param>
    /// <returns></returns>
    public static BigNumber Pow(BigNumber a, int b) => Pow(a, new BigInteger(b));

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="a">底数</param>
    /// <param name="b">指数</param>
    /// <returns></returns>
    public static BigNumber Pow(BigNumber a, uint b) => Pow(a, new BigInteger(b));

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="a">底数</param>
    /// <param name="b">指数</param>
    /// <returns></returns>
    public static BigNumber Pow(BigNumber a, long b) => Pow(a, new BigInteger(b));

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="b">指数</param>
    public void Pow(BigInteger b) => SetBigNumber(Pow(this, b));

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="b">指数</param>
    public void Pow(int b) => SetBigNumber(Pow(this, new BigInteger(b)));

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="b">指数</param>
    public void Pow(uint b) => SetBigNumber(Pow(this, new BigInteger(b)));

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="b">指数</param>
    public void Pow(long b) => SetBigNumber(Pow(this, new BigInteger(b)));

    /// <summary>
    /// 判断两个BigNumber是否相等
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Equal(BigNumber a, BigNumber b)
    {
        //a.ToAssociative();
        //b.ToAssociative();
        //var w = a.whole == b.whole;
        //var n = a.numerator == b.numerator;
        //var d = a.denominator == b.denominator;
        //return w && n && d;
        // ab先通分成一样的分母后再比较分子大小即可,上面的方式会造成分子分母不一定一样,比如:1/2和2/4;
        var a_temp = ((a.Whole * a.Denominator) + a.Numerator) * b.Denominator;
        var b_temp = ((b.Whole * b.Denominator) + b.Numerator) * a.Denominator;
        return a_temp == b_temp;
    }

    /// <summary>
    /// 判断两个BigNumber是否不相等
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool NotEqual(BigNumber a, BigNumber b) => !Equal(a, b);

    /// <summary>
    /// 判断一个BigNumber是否小于另一个BigNumber
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Smaller(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;

    /// <summary>
    /// 判断一个BigNumber是否小于等于另一个BigNumber
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool SmallerOrEqual(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;

    /// <summary>
    /// 判断一个BigNumber是否大于另一个BigNumber
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Bigger(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;

    /// <summary>
    /// 判断一个BigNumber是否大于等于另一个BigNumber
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool BiggerOrEqual(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;

    #endregion

    /// <summary>
    /// 计算最大公因数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static BigInteger GreatestCommonFactor(BigInteger a, BigInteger b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
            {
                a %= b;
            }
            else
            {
                b %= a;
            }
        }
        return a | b;
    }

    /// <summary>
    /// 简化BigNumber
    /// </summary>
    private void Simplify()
    {
        if (Denominator == 0)
        {
            throw new DivideByZeroException();
        }
        var num = (Whole * Denominator) + Numerator;
        var den = Denominator;
        if (den < 0)
        {
            num = -num;
            den = -den;
        }
        var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(num), den);
        num /= gcd;
        den /= gcd;
        Whole = num / den;
        Numerator = num % den;
        Denominator = den;
        Sign = num == 0
                   ? 0
                   : num > 0
                       ? 1
                       : -1;
    }

    /// <summary>
    /// 转换为绝对值
    /// </summary>
    public void ToAbs()
    {
        Numerator = BigInteger.Abs(Numerator);
        Whole = BigInteger.Abs(Whole);
        Denominator = BigInteger.Abs(Denominator);
        Sign = Whole == 0 && Numerator == 0 ? 0 : 1;
    }

    /// <summary>
    /// 计算BigNumber的绝对值
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static BigNumber Abs(BigNumber a)
    {
        var clone = a.CloneRaw();
        clone.ToAbs();
        return clone;
    }

    /// <summary>
    /// 获取符号位
    /// </summary>
    /// <returns></returns>
    public int GetSign()
    {
        var sign = 0;
        if (Whole.Sign != 0 && Numerator.Sign != 0 && Denominator.Sign != 0)
        {
            sign = Whole.Sign * Numerator.Sign * Denominator.Sign;
        }
        else if (Whole.Sign != 0 && Numerator != 0)
        {
            sign = Whole.Sign * Numerator.Sign;
        }
        else if (Whole.Sign != 0 && Denominator.Sign != 0)
        {
            sign = Whole.Sign * Denominator.Sign;
        }
        else if (Numerator.Sign != 0 && Denominator.Sign != 0)
        {
            sign = Numerator.Sign * Denominator.Sign;
        }
        return sign;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        Simplify();
        if (Sign == 0)
        {
            return "0";
        }
        var signPrefix = Sign == -1 ? "-" : "";
        return Numerator == 0
                   ? $"{signPrefix}{BigInteger.Abs(Whole)}"
                   : Whole == 0
                       ? $"{signPrefix}{BigInteger.Abs(Numerator)}/{Denominator}"
                       : $"{signPrefix}{BigInteger.Abs(Whole)} {BigInteger.Abs(Numerator)}/{Denominator}";
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is not null && (ReferenceEquals(this, obj) || (obj is BigNumber other && Equals(other)));

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        Simplify();
        var num = (Whole * Denominator) + Numerator;
        return HashCode.Combine(num, Denominator);
    }

    /// <inheritdoc />
    public bool Equals(BigNumber? other) => other is not null && CompareTo(other) == 0;

    /// <inheritdoc />
    public int CompareTo(BigNumber? other)
    {
        if (other is null)
        {
            return 1;
        }
        var numA = (Whole * Denominator) + Numerator;
        var denA = Denominator;
        var numB = (other.Whole * other.Denominator) + other.Numerator;
        var denB = other.Denominator;
        return (numA * denB).CompareTo(numB * denA);
    }

    /// <summary>
    /// is BigNumber an integer?
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool IsInteger(out BigInteger value)
    {
        Simplify();
        if (Numerator == 0)
        {
            value = Whole;
            return true;
        }
        value = 0;
        return false;
    }

    #endregion
}