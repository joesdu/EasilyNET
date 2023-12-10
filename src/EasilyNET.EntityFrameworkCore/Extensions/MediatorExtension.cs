namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 扩展
/// </summary>
public static class MediatorExtension
{
    /// <summary>
    /// 异步调度域事件
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, DefaultDbContext ctx, CancellationToken cancellationToken = default)
    {
        await ctx.ChangeTracker.Entries<IGenerateDomainEvents>()
                 .Where(o => o.Entity.GetDomainEvents() is not null && o.Entity.GetDomainEvents()!.Any()).Select(o => o.Entity)
                 .SelectMany(o =>
                 {
                     var domainEvents = o.GetDomainEvents()!.ToList();
                     o.ClearDomainEvent();
                     return domainEvents;
                 }).ForeachAsync(async e => await mediator.Publish(e, cancellationToken), cancellationToken);
    }
}