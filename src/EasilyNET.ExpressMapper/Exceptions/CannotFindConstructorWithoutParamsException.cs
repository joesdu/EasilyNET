namespace EasilyNET.ExpressMapper.Exceptions;

/// <summary>
/// 无法找到无参数构造函数异常类。
/// Exception class for cases where a parameterless constructor cannot be found.
/// </summary>
public class CannotFindConstructorWithoutParamsException : Exception
{
    /// <summary>
    /// 搜索的类型。
    /// The type that was searched.
    /// </summary>
    public readonly Type SearchedType;

    /// <summary>
    /// 初始化 <see cref="CannotFindConstructorWithoutParamsException" /> 类的新实例。
    /// Initializes a new instance of the <see cref="CannotFindConstructorWithoutParamsException" /> class.
    /// </summary>
    /// <param name="searchedType">搜索的类型。The type that was searched.</param>
    public CannotFindConstructorWithoutParamsException(Type searchedType)
    {
        SearchedType = searchedType;
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="CannotFindConstructorWithoutParamsException" /> 类的新实例。
    /// Initializes a new instance of the <see cref="CannotFindConstructorWithoutParamsException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">描述错误的消息。The message that describes the error.</param>
    /// <param name="searchedType">搜索的类型。The type that was searched.</param>
    public CannotFindConstructorWithoutParamsException(string message, Type searchedType) : base(message)
    {
        SearchedType = searchedType;
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="CannotFindConstructorWithoutParamsException" /> 类的新实例。
    /// Initializes a new instance of the <see cref="CannotFindConstructorWithoutParamsException" /> class with a specified error message and a reference to
    /// the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">描述错误的消息。The message that describes the error.</param>
    /// <param name="inner">导致当前异常的异常。The exception that is the cause of the current exception.</param>
    /// <param name="searchedType">搜索的类型。The type that was searched.</param>
    public CannotFindConstructorWithoutParamsException(string message, Exception inner, Type searchedType) : base(message, inner)
    {
        SearchedType = searchedType;
    }
}