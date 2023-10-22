using EasilyNET.EntityFrameworkCore.Optiions;
using EasilyNET.EntityFrameworkCore.Repositories;

namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 服务集合扩展
/// </summary>
public static class ServiceCollectionExtensions
{


 
    /// <summary>
    /// 添加EFCore添加上下文
    /// </summary>
    /// <param name="services">服务</param>
    /// <param name="setupAction">配置</param>
    /// <typeparam name="TDbContext">上下文</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddEFCore<TDbContext>(this IServiceCollection services, Action<EFCoreOptions> setupAction)
        where TDbContext : DefaultDbContext
    {

        return services.AddEFCore<TDbContext>((_, b) => setupAction(b));
    }
    /// <summary>
    /// 添加EFCore添加上下文
    /// </summary>
    /// <param name="services">服务</param>
    /// <param name="setupAction">配置</param>
    /// <typeparam name="TDbContext">上下文</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddEFCore<TDbContext>(this IServiceCollection services, Action<IServiceProvider,EFCoreOptions> setupAction)
        where TDbContext : DefaultDbContext
    {

        setupAction.NotNull(nameof(setupAction));

        var efCoreOptions = new EFCoreOptions();

        
        if ((efCoreOptions.ConfigureDbContextBuilder!) is null)
        {
            efCoreOptions.ConfigureDbContextBuilder.NotNull(nameof(efCoreOptions.ConfigureDbContextBuilder)); 
        }

        services.AddSingleton<EFCoreOptions>(sp =>
        {
            setupAction(sp, efCoreOptions);
            return efCoreOptions;
        });    
        services.AddDbContext<DefaultDbContext, TDbContext>(builder =>
        {
            efCoreOptions.ConfigureDbContextBuilder!(builder);
        });
        
        services.AddUnitOfWork<TDbContext>();
        return services;
    }
    


    /// <summary>
    /// 添加工作单元
    /// </summary>
    /// <param name="services">服务</param>
    /// <typeparam name="TDbContext">上下文</typeparam>
    /// <returns></returns>
    private static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DefaultDbContext
    {
        services.AddScoped<IUnitOfWork>(p => p.GetRequiredService<TDbContext>());
        return services;
    }

    /// <summary>
    /// 添加默认仓储
    /// </summary>
    /// <param name="services">服务</param>
    /// <returns></returns>
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        return services;
    }
}