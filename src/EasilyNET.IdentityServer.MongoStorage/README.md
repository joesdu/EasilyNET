#### IdentityServer 7.x Data Persistence for MongoDB

**参考项目** [Github](https://github.com/diogodamiani/IdentityServer4.Contrib.MongoDB)

**寻求帮助**: 希望能有懂 Razor 的同学帮我根据官方的 QuickUI 写一个管理页面, 若是能教我 Razor 的话那就更 Nice 了.

- 本地使用 docker 启动 MongoDB 服务

```bash
docker run --name mongo1 -p 27017:27017 -d --rm -it -e MONGO_INITDB_ROOT_USERNAME=guest -e MONGO_INITDB_ROOT_PASSWORD="guest" mongo:latest
```

###### 如何使用.

- Install Package

```shell
Install-Package EasilyNET.IdentityServer.MongoStorage
```

- appsettings.json 内容

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://guest:guest@127.0.1:27017/?authSource=admin&serverSelectionTimeoutMS=1000"
  }
}
```

- Add the following code to the Program.cs file in the root of the project

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentityServer(options =>
       {
           options.Events.RaiseErrorEvents = true;
           options.Events.RaiseInformationEvents = true;
           options.Events.RaiseFailureEvents = true;
           options.Events.RaiseSuccessEvents = true;
           // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
           options.EmitStaticAudienceClaim = true;
       })
       .AddConfigurationStore(c =>
       {
           c.ConnectionString = builder.Configuration.GetConnectionString("MongoDB")!;
           c.Database = "IdentityServer";
       })
       .AddOperationalStore(tco =>
       {
           tco.Enable = true;
           tco.Interval = 3600;
       })
       // 其他配置
       //.AddCustomTokenRequestValidator<CustomTokenRequestValidator>()
       .AddDeveloperSigningCredential();

builder.Services.AddAuthentication()
       .AddOpenIdConnect("demoidsrv", "IdentityServer", options =>
       {
           options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
           options.SignOutScheme = IdentityServerConstants.SignoutScheme;
           options.Authority = "https://demo.identityserver.io/";
           options.ClientId = "implicit";
           options.ResponseType = "id_token";
           options.SaveTokens = true;
           options.CallbackPath = new("/signin-idsrv");
           options.SignedOutCallbackPath = new("/signout-callback-idsrv");
           options.RemoteSignOutPath = new("/signout-idsrv");
           options.TokenValidationParameters = new() { NameClaimType = "name", RoleClaimType = "role" };
       });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();
app.UseRouting();

// this seeding is only for the template to bootstrap the DB and users.
// in production you will likely want a different approach.
// 初次运行发送配置数据到数据库
using (var scop = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    SendData.EnsureSeedData(scop.ServiceProvider.GetRequiredService<IConfigurationDbContext>());
}
app.UseIdentityServer();
app.UseAuthorization();
app.Run();
```

```csharp
internal static class SendData
{
    internal static void EnsureSeedData(IConfigurationDbContext context)
    {
        if (!context.Clients.Any())
        {
            foreach (var client in Config.Clients)
            {
                context.AddClient(client.ToEntity());
            }
        }
        if (!context.IdentityResources.Any())
        {
            foreach (var resource in Config.IdentityResources)
            {
                context.AddIdentityResource(resource.ToEntity());
            }
        }
        if (!context.ApiResources.Any())
        {
            foreach (var resource in Config.ApiResources)
            {
                context.AddApiResource(resource.ToEntity());
            }
        }
        if (!context.ApiScopes.Any())
        {
            foreach (var resource in Config.ApiScopes)
            {
                context.AddApiScope(resource.ToEntity());
            }
        }
    }
}
```
