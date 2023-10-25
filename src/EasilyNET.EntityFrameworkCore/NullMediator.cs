namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 空的Mediator
/// </summary>
public sealed class NullMediator : IMediator
{
    /// <summary>
    /// 实例
    /// </summary>
    public static readonly NullMediator Instance = new();

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = new()) => Task.FromResult<TResponse>(default!);

    /// <inheritdoc />
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new()) where TRequest : IRequest => Task.FromResult(false);

    /// <inheritdoc />
    public Task<object?> Send(object request, CancellationToken cancellationToken = new()) => Task.FromResult(default(object?));

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = new()) => default!;

    /// <inheritdoc />
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = new()) => default!;

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = new()) => Task.CompletedTask;

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = new()) where TNotification : INotification => Task.CompletedTask;
}