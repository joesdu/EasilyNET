using EasilyNET.EntityFrameworkCore.Optiions;

namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 服务集合扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加EF CORE上下文
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="optionsAction"></param>
    /// <typeparam name="TDbContext"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddDefaultDbContext<TDbContext> (this IServiceCollection serviceCollection,Action<DbContextOptionsBuilder> optionsAction)
    where TDbContext:DefaultDbContext
    {
        serviceCollection.AddDbContext<DefaultDbContext, TDbContext>(optionsAction);
        return serviceCollection;
    }
    
    /// <summary>
    /// 添加EF CORE上下文
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="optionsAction"></param>
    /// <typeparam name="TDbContext"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddDefaultDbContext<TDbContext> (this IServiceCollection serviceCollection,Action<EFCoreOptions>? easilyNETDbContextOptions=null)
        where TDbContext:DefaultDbContext
    {
        EFCoreOptions options = default!;
        //判断是否为空，这样可以减少new
        if (easilyNETDbContextOptions is not null) { 
            options = new EFCoreOptions();
            easilyNETDbContextOptions?.Invoke(options);
            serviceCollection.AddSingleton<EFCoreOptions>(options);
        }
  
    
        serviceCollection.AddDbContext<DefaultDbContext, TDbContext>(options?.DefaultDbContextOptionsAction);
        return serviceCollection;
    }

    
    /// <summary>
    /// 添加工作单元
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TDbContext"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DefaultDbContext
    {
        services.AddScoped<IUnitOfWork>(p => p.GetRequiredService<TDbContext>());
        return services;
    }

}