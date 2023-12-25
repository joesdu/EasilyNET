// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.AutoInjection.SourceGenerator.MsTest.Tests;

/// <summary>
/// </summary>
internal sealed class TestAppModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddAutoInjection();
        //context.Services.addTo();
    }
}