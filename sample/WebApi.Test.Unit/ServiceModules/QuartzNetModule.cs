using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Core.BaseType;
using Quartz;
using Quartz.AspNetCore;
using WebApi.Test.Unit.QzJobs;

namespace WebApi.Test.Unit;

/// <inheritdoc />
public class QuartzNetModule : AppModule
{
    /// <inheritdoc />
    public QuartzNetModule()
    {
        Enable = false;
    }

    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        // 由于Qz的策略,我们这里只能通过这种显示的方式注入服务.才能在IJob中使用属性注入.
        context.Services.AddScoped<PropertyInjectionTestJob>();
        context.Services.AddQuartzServer(o =>
        {
            // when shutting down we want jobs to complete gracefully
            o.WaitForJobsToComplete = true;
            // 默认就是 true,这里写明是为了规避IDE的代码风格提示.
            o.AwaitApplicationStarted = true;
        }).AddQuartz(q =>
        {
            q.SchedulerId = SnowId.GenerateNewId().ToString();
            q.SchedulerName = "WebApi.Test.Unit.Quartz";
            //绑定Job和Trigger 这个东西那么难用，为什么还要用。。。。。。。。。
            q.ScheduleJob<PropertyInjectionTestJob>(t =>
                t.WithIdentity(nameof(PropertyInjectionTestJob))
                 .StartNow()
                 .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever())
                 .WithDescription("属性注入测试"));
        });
    }
}