using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.WebCore.Filters;
using EasilyNET.WebCore.JsonConverters;
using System.Text.Json.Serialization;

namespace MongoGridFS.Example;

/// <summary>
/// 注册一些控制器的基本内容
/// </summary>
public class ControllersModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        _ = context.Services.AddControllers(x =>
        {
            _ = x.Filters.Add<ActionExecuteFilter>();
            _ = x.Filters.Add<ExceptionFilter>();
        }).AddJsonOptions(c =>
        {
            c.JsonSerializerOptions.Converters.Add(new DecimalNullConverter());
            c.JsonSerializerOptions.Converters.Add(new IntNullConverter());
            c.JsonSerializerOptions.Converters.Add(new BoolNullConverter());
            c.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
            c.JsonSerializerOptions.Converters.Add(new DateTimeNullConverter());
            c.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
            c.JsonSerializerOptions.Converters.Add(new TimeOnlyNullJsonConverter());
            c.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
            c.JsonSerializerOptions.Converters.Add(new DateOnlyNullJsonConverter());
            c.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        _ = context.Services.AddEndpointsApiExplorer();
    }
}