// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en"><see cref="DateOnly" /> | <see cref="TimeOnly" /> extensions</para>
///     <para xml:lang="zh"><see cref="DateOnly" /> | <see cref="TimeOnly" /> 扩展</para>
/// </summary>
public static class DateTimeOnlyExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Converts ticks to <see cref="TimeOnly" /></para>
    ///     <para xml:lang="zh">从 Ticks 转换为 <see cref="TimeOnly" /></para>
    /// </summary>
    /// <param name="ticks">
    ///     <para xml:lang="en">The ticks to convert</para>
    ///     <para xml:lang="zh">要转换的 Ticks</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The corresponding <see cref="TimeOnly" /></para>
    ///     <para xml:lang="zh">对应的 <see cref="TimeOnly" /></para>
    /// </returns>
    public static TimeOnly ToTimeOnly(this long ticks) => TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks));

    /// <summary>
    ///     <para xml:lang="en">Converts <see cref="DateTime" /> to <see cref="TimeOnly" /></para>
    ///     <para xml:lang="zh">将 <see cref="DateTime" /> 转换为 <see cref="TimeOnly" /></para>
    /// </summary>
    /// <param name="dateTime">
    ///     <para xml:lang="en">The <see cref="DateTime" /> to convert</para>
    ///     <para xml:lang="zh">要转换的 <see cref="DateTime" /></para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The corresponding <see cref="TimeOnly" /></para>
    ///     <para xml:lang="zh">对应的 <see cref="TimeOnly" /></para>
    /// </returns>
    public static TimeOnly ToTimeOnly(this DateTime dateTime) => TimeOnly.FromDateTime(dateTime);

    /// <summary>
    ///     <para xml:lang="en">Converts ticks to <see cref="DateOnly" /></para>
    ///     <para xml:lang="zh">从 Ticks 转换为 <see cref="DateOnly" /></para>
    /// </summary>
    /// <param name="ticks">
    ///     <para xml:lang="en">The ticks to convert</para>
    ///     <para xml:lang="zh">要转换的 Ticks</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The corresponding <see cref="DateOnly" /></para>
    ///     <para xml:lang="zh">对应的 <see cref="DateOnly" /></para>
    /// </returns>
    public static DateOnly ToDateOnly(this long ticks) => DateOnly.FromDateTime(new(ticks, DateTimeKind.Local));

    /// <summary>
    ///     <para xml:lang="en">Converts <see cref="DateTime" /> to <see cref="DateOnly" /></para>
    ///     <para xml:lang="zh">将 <see cref="DateTime" /> 转换为 <see cref="DateOnly" /></para>
    /// </summary>
    /// <param name="dateTime">
    ///     <para xml:lang="en">The <see cref="DateTime" /> to convert</para>
    ///     <para xml:lang="zh">要转换的 <see cref="DateTime" /></para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The corresponding <see cref="DateOnly" /></para>
    ///     <para xml:lang="zh">对应的 <see cref="DateOnly" /></para>
    /// </returns>
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDateTime(dateTime);
}