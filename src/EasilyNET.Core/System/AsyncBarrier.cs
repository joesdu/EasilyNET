// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using EasilyNET.Core.Threading;

namespace EasilyNET.Core.System;

/// <summary>
/// 表示一个异步屏障,它会阻塞一组任务,直到所有任务都到达屏障,通常用于需要协调多个异步任务的场景,确保所有任务在某个同步点之前都已完成特定的工作,然后再继续执行
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
/// var barrier = new AsyncBarrier(3);
/// var cts = new CancellationTokenSource();
/// 
/// // 启动三个任务，每个任务都会在屏障处等待
/// var tasks = new List&lt;Task&gt;
/// {
///     Task.Run(async () =>
///     {
///         Console.WriteLine("任务1到达屏障");
///         await barrier.SignalAndWait(cts.Token);
///         Console.WriteLine("任务1继续执行");
///     }),
///     Task.Run(async () =>
///     {
///         Console.WriteLine("任务2到达屏障");
///         await barrier.SignalAndWait(cts.Token);
///         Console.WriteLine("任务2继续执行");
///     }),
///     Task.Run(async () =>
///     {
///         Console.WriteLine("任务3到达屏障");
///         await barrier.SignalAndWait(cts.Token);
///         Console.WriteLine("任务3继续执行");
///     })
/// };
/// 
/// // 等待所有任务完成
/// await Task.WhenAll(tasks);
///   ]]>
/// </code>
/// </example>
public sealed class AsyncBarrier
{
    private readonly int participantCount;
    private readonly SyncLock syncRoot = new();
    private readonly Stack<Waiter> waiters;

    /// <summary>
    /// 使用指定数量的参与者初始化 <see cref="AsyncBarrier" /> 类的新实例
    /// </summary>
    /// <param name="participants">屏障的参与者数量</param>
    /// <exception cref="ArgumentOutOfRangeException">当参与者数量小于或等于零时抛出</exception>
    public AsyncBarrier(int participants)
    {
        if (participants <= 0)
            throw new ArgumentOutOfRangeException(nameof(participants), $"参数 {nameof(participants)} 必须是一个正数。");
        participantCount = participants;
        waiters = new(participants - 1);
    }

    /// <summary>
    /// 表示一个参与者已到达屏障,并等待所有其他参与者到达屏障
    /// </summary>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public ValueTask SignalAndWait(CancellationToken cancellationToken)
    {
        using (syncRoot.Lock())
        {
            if (waiters.Count + 1 == participantCount)
            {
                while (waiters.Count > 0)
                {
                    var waiter = waiters.Pop();
                    waiter.CompletionSource.TrySetResult(default);
                    waiter.CancellationRegistration.Dispose();
                }
                return new(cancellationToken.IsCancellationRequested
                               ? Task.FromCanceled(cancellationToken)
                               : Task.CompletedTask);
            }
            TaskCompletionSource<EmptyStruct> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var ctr = cancellationToken.CanBeCanceled
                          ? cancellationToken.Register(static (tcs, ct) => ((TaskCompletionSource<EmptyStruct>)tcs!).TrySetCanceled(ct), tcs)
                          : default;
            waiters.Push(new(tcs, ctr));
            return new(tcs.Task);
        }
    }

    private readonly struct Waiter(TaskCompletionSource<EmptyStruct> completionSource, CancellationTokenRegistration cancellationRegistration)
    {
        internal TaskCompletionSource<EmptyStruct> CompletionSource => completionSource;

        internal CancellationTokenRegistration CancellationRegistration => cancellationRegistration;
    }
}