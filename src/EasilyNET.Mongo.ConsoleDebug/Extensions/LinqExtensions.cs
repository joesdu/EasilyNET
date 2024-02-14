namespace EasilyNET.Mongo.ConsoleDebug.Extensions;

/// <summary>
/// LinqExtensions
/// </summary>
internal static class LinqExtensions
{
    /// <summary>
    /// Exec
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    internal static T Exec<T>(this T t, Func<T, T> predicate) => predicate(t);
}
