using MediatR;

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
    public static  Task DispatchDomainEventsAsync(this IMediator mediator, DefaultDbContext ctx,CancellationToken cancellationToken = default)
    {
        var domainEntities = ctx.ChangeTracker.Entries<Entity>()
                                .Where(o =>o.Entity.DomainEvents !=null &&o.Entity.DomainEvents!.Any()).ToList();
        var domainEvents = domainEntities?
                           .SelectMany(x => x.Entity.DomainEvents!)
                           .ToList();
        domainEntities?.ToList().ForEach(o => o.Entity.ClearDomainEvent());
        domainEvents?.ForAsync(async (e, index) =>
        {
            await mediator.Publish(e, cancellationToken)!;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}