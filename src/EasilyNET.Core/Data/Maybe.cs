

using System.Diagnostics.CodeAnalysis;

namespace EasilyNET.Core.Data;

//参数文章 https://enterprisecraftsmanship.com/posts/functional-c-non-nullable-reference-types/

/// <summary>
/// 优雅处理空值
/// </summary>
  public struct Maybe<T> : IEquatable<Maybe<T>>
        where T : class
    {
        private readonly T _value;

        private Maybe(T value)
        {
            _value = value;
        }

        /// <summary>
        /// 是否有值
        /// </summary>
        public bool HasValue => _value != null;
        
        /// <summary>
        /// 得到值
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public T Value => _value ?? throw new InvalidOperationException();
        
        /// <summary>
        /// 没有
        /// </summary>
        public static Maybe<T> None => new Maybe<T>();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Maybe<T>(T value)
        {
            return new Maybe<T>(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maybe"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool operator ==(Maybe<T> maybe, T value)
        {
            return maybe.HasValue && maybe.Value.Equals(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maybe"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool operator !=(Maybe<T> maybe, T value)
        {
            return !(maybe == value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(Maybe<T> left, Maybe<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(Maybe<T> left, Maybe<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Maybe<T> other)
        {
            if (!HasValue && !other.HasValue)
                return true;

            if (!HasValue || !other.HasValue)
                return false;

            return _value.Equals(other.Value);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>

        public override bool Equals(object obj)
        {
            if (obj is T typed)
            {
                obj = new Maybe<T>(typed);
            }

            if (!(obj is Maybe<T> other)) return false;

            return Equals(other);
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HasValue ? _value.GetHashCode() : default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return HasValue ? _value.ToString() : "NO VALUE";
        }
    }



