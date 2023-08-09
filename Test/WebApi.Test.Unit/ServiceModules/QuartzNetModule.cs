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
    public override void ConfigureServices(ConfigureServicesContext context)
    {
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
                 .WithSimpleSchedule(c => c.WithIntervalInSeconds(10))
                 .WithDescription("属性注入测试"));
        });
    }
}