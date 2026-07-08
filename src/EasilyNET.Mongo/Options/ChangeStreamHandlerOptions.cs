// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using EasilyNET.Mongo.ChangeStreams;
using MongoDB.Driver;

namespace EasilyNET.Mongo.Options;

/// <summary>
///     <para xml:lang="en">Configuration options for <see cref="MongoChangeStreamHandler{TDocument}" /></para>
///     <para xml:lang="zh"><see cref="MongoChangeStreamHandler{TDocument}" /> 的配置选项</para>
/// </summary>
public sealed class ChangeStreamHandlerOptions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Maximum number of retry attempts when the change stream cursor is invalidated or a transient error occurs.
    ///     Defaults to <c>5</c>. Set to <c>0</c> to disable retries.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     当变更流游标失效或发生瞬态错误时的最大重试次数。
    ///     默认为 <c>5</c>。设置为 <c>0</c> 以禁用重试。
    ///     </para>
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    ///     <para xml:lang="en">
    ///     The initial delay between retry attempts. The delay increases exponentially with each attempt.
    ///     Defaults to <c>2 seconds</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     重试之间的初始延迟。延迟随每次尝试呈指数增长。
    ///     默认为 <c>2 秒</c>。
    ///     </para>
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     <para xml:lang="en">
    ///     The maximum delay between retry attempts.
    ///     Defaults to <c>60 seconds</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     重试之间的最大延迟。默认为 <c>60 秒</c>。
    ///     </para>
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether to persist resume tokens to a MongoDB collection for surviving application restarts.
    ///     When <see langword="false" />, resume tokens are kept in memory only.
    ///     Defaults to <see langword="false" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否将恢复令牌持久化到 MongoDB 集合中以在应用程序重启后恢复。
    ///     当为 <see langword="false" /> 时，恢复令牌仅保存在内存中。
    ///     默认为 <see langword="false" />。
    ///     </para>
    /// </summary>
    public bool PersistResumeToken { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Minimum interval between resume-token persistence writes.
    ///     When <see cref="TimeSpan.Zero" /> (default), the token is persisted after every successfully handled event —
    ///     safest, but adds one MongoDB write per event. Under high change volume, set e.g. <c>TimeSpan.FromSeconds(5)</c>
    ///     to throttle writes; tokens arriving within the interval are kept in memory and flushed when the stream
    ///     pauses, errors, or shuts down. A larger interval means more events may be re-delivered after a crash
    ///     (processing is at-least-once either way).
    ///     Only used when <see cref="PersistResumeToken" /> is <see langword="true" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     恢复令牌持久化写入的最小间隔。
    ///     为 <see cref="TimeSpan.Zero" />（默认）时，每个成功处理的事件后都会持久化令牌 —
    ///     最安全，但每个事件都会增加一次 MongoDB 写入。在高变更量场景下，可设置为例如 <c>TimeSpan.FromSeconds(5)</c>
    ///     来限流写入；间隔内到达的令牌保存在内存中，并在流暂停、出错或关闭时刷新写入。
    ///     间隔越大，崩溃后可能重复投递的事件越多（无论如何处理语义都是至少一次）。
    ///     仅在 <see cref="PersistResumeToken" /> 为 <see langword="true" /> 时使用。
    ///     </para>
    /// </summary>
    public TimeSpan ResumeTokenPersistInterval { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the collection used to store resume tokens.
    ///     Only used when <see cref="PersistResumeToken" /> is <see langword="true" />.
    ///     Defaults to <c>"_changeStreamResumeTokens"</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     用于存储恢复令牌的集合名称。
    ///     仅在 <see cref="PersistResumeToken" /> 为 <see langword="true" /> 时使用。
    ///     默认为 <c>"_changeStreamResumeTokens"</c>。
    ///     </para>
    /// </summary>
    public string ResumeTokenCollectionName { get; set; } = "_changeStreamResumeTokens";

    /// <summary>
    ///     <para xml:lang="en">
    ///     Controls whether and how full updated documents are returned for update operations.
    ///     Defaults to <see cref="ChangeStreamFullDocumentOption.UpdateLookup" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     控制更新操作是否以及如何返回完整文档。
    ///     默认为 <see cref="ChangeStreamFullDocumentOption.UpdateLookup" />。
    ///     </para>
    /// </summary>
    public ChangeStreamFullDocumentOption FullDocument { get; set; } = ChangeStreamFullDocumentOption.UpdateLookup;
}