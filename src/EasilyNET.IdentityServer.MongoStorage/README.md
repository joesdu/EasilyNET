#### IdentityServer 6.x Data Persistence for MongoDB

###### 如何使用.

1. Install Package

```shell
Install-Package EasilyNET.IdentityServer.MongoStorage
```

2. Add the following code to the Program.cs file in the root of the project

```csharp
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
})
    .AddMongoRepository()
    .AddDeveloperSigningCredential()
    .AddIdentityClients()
    .AddIdentityResources()
    .AddIdentityPersistedGrants()
    .AddPolicyService();

//.AddCustomTokenRequestValidator<CustomTokenRequestValidator>()
//// not recommended for production - you need to store your key material somewhere secure

// Create initial resources from Identity Default Config.cs
SeedDatabase.Seed(builder.Services);
```

- add SeedDatabase class in your server project

```csharp
public static void Seed(IServiceCollection services)
{
    var sp = services.BuildServiceProvider();
    var repository = sp.GetService<IRepository>()!;
    if (repository?.All<Client>().Count() == 0)
    {
        foreach (var client in IdentityConfig.GetClients())
        {
            repository.Add(client);
        }
    }
    if (repository?.All<ApiScope>().Count() == 0)
    {
        foreach (var scopes in IdentityConfig.GetApiScopes())
        {
            repository.Add(scopes);
        }
    }
    if (repository?.All<IdentityResource>().Count() == 0)
    {
        foreach (var resource in IdentityConfig.GetIdentityResources())
        {
            repository.Add(resource);
        }
    }
    if (repository?.All<ApiResource>().Count() == 0)
    {
        foreach (var api in IdentityConfig.GetApiResources())
        {
            repository.Add(api);
        }
    }
    if (repository?.All<TestUser>().Count() == 0)
    {
        foreach (var user in TestUsers.Users)
        {
            repository.Add(user);
        }
    }
}
```
