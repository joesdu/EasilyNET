// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Threading;

/// <summary>
///     <para xml:lang="en">
///     Represents an asynchronous barrier that blocks a group of tasks until all tasks have reached the barrier. Typically used in
///     scenarios where multiple asynchronous tasks need to be coordinated to ensure all tasks have completed specific work before continuing.
///     </para>
///     <para xml:lang="zh">表示一个异步屏障，它会阻塞一组任务，直到所有任务都到达屏障。通常用于需要协调多个异步任务的场景，确保所有任务在某个同步点之前都已完成特定的工作，然后再继续执行。</para>
///     <example>
///         <code>
/// <![CDATA[
/// var barrier = new AsyncBarrier(3);
/// var cts = new CancellationTokenSource();
/// 
/// // 启动三个任务，每个任务都会在屏障处等待
/// var tasks = new List<Task>
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
///     </example>
/// </summary>
public sealed class AsyncBarrier
{
    private readonly int participantCount;
    private readonly Lock syncRoot = new();

    // A List (not a Stack) so a waiter whose token is canceled can be removed by reference; otherwise a
    // canceled waiter keeps occupying a participant slot, inflating the count and deadlocking the barrier.
    private readonly List<Waiter> waiters;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="AsyncBarrier" /> class with the specified number of participants</para>
    ///     <para xml:lang="zh">使用指定数量的参与者初始化 <see cref="AsyncBarrier" /> 类的新实例</para>
    /// </summary>
    /// <param name="participants">
    ///     <para xml:lang="en">The number of participants for the barrier</para>
    ///     <para xml:lang="zh">屏障的参与者数量</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the number of participants is less than or equal to zero</para>
    ///     <para xml:lang="zh">当参与者数量小于或等于零时抛出</para>
    /// </exception>
    public AsyncBarrier(int participants)
    {
        if (participants <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(participants), $"参数 {nameof(participants)} 必须是一个正数。");
        }
        participantCount = participants;
        waiters = new(participants - 1);
    }

    private void RemoveCanceledWaiter(Waiter waiter, CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            // Free the canceled waiter's slot. Do NOT dispose the registration here: we may be running inside
            // its own callback, and CancellationTokenRegistration.Dispose() would block waiting for itself.
            if (waiters.Remove(waiter))
            {
                waiter.CompletionSource.TrySetCanceled(cancellationToken);
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Signals that a participant has reached the barrier and waits for all other participants to reach the barrier</para>
    ///     <para xml:lang="zh">表示一个参与者已到达屏障，并等待所有其他参与者到达屏障</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The token to monitor for cancellation requests</para>
    ///     <para xml:lang="zh">用于监视取消请求的令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
    public ValueTask SignalAndWait(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            if (waiters.Count + 1 == participantCount)
            {
                foreach (var w in waiters)
                {
                    w.CompletionSource.TrySetResult(default);
                    // Unregister (not Dispose) so we never block on a cancellation callback that is waiting for
                    // this same lock; a non-fired registration is simply detached.
                    w.CancellationRegistration.Unregister();
                }
                waiters.Clear();
                return new(cancellationToken.IsCancellationRequested
                               ? Task.FromCanceled(cancellationToken)
                               : Task.CompletedTask);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return new(Task.FromCanceled(cancellationToken));
            }
            TaskCompletionSource<EmptyStruct> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var waiter = new Waiter(tcs);
            waiters.Add(waiter);
            if (cancellationToken.CanBeCanceled)
            {
                // syncRoot is reentrant, so a registration that fires synchronously here is safe.
                waiter.CancellationRegistration = cancellationToken.Register(() => RemoveCanceledWaiter(waiter, cancellationToken));
            }
            return new(tcs.Task);
        }
    }

    private sealed class Waiter(TaskCompletionSource<EmptyStruct> completionSource)
    {
        internal TaskCompletionSource<EmptyStruct> CompletionSource => completionSource;

        internal CancellationTokenRegistration CancellationRegistration { get; set; }
    }
}

/// <summary>
///     <para xml:lang="en">An empty struct</para>
///     <para xml:lang="zh">一个空结构体</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">When a generic type requires a type parameter but does not use it, this can save 4 bytes compared to System.Object</para>
///     <para xml:lang="zh">当泛型类型需要一个类型参数但完全不使用时，这可以节省 4 个字节，相对于 System.Object</para>
/// </remarks>
internal readonly struct EmptyStruct
{
    /// <summary>
    ///     <para xml:lang="en">Gets an instance of the empty struct</para>
    ///     <para xml:lang="zh">获取空结构体的一个实例</para>
    /// </summary>
    internal static EmptyStruct Instance => default;
}