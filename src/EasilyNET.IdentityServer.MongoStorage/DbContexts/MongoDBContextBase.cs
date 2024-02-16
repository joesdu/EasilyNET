using EasilyNET.IdentityServer.MongoStorage.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EasilyNET.IdentityServer.MongoStorage.DbContexts;

/// <summary>
/// MongoDBContextBase
/// </summary>
public class MongoDBContextBase : IDisposable
{
    /// <summary>
    /// 构造函数
    /// 构造函数
    /// </summary>
    /// <param name="settings"></param>
    /// <exception cref="ArgumentNullException"></exception>
    protected MongoDBContextBase(IOptions<MongoDBConfiguration> settings)
    {
        if (settings.Value is null)
        {
            throw new ArgumentNullException(nameof(settings), "MongoDBConfiguration cannot be null.");
        }
        if (settings.Value.ConnectionString is null)
        {
            throw new ArgumentNullException(nameof(settings), "MongoDBConfiguration.ConnectionString cannot be null.");
        }
        var mongoUrl = MongoUrl.Create(settings.Value.ConnectionString);
        if (settings.Value.Database is null && mongoUrl.DatabaseName is null)
        {
            throw new ArgumentNullException(nameof(settings), "MongoDBConfiguration.Database cannot be null.");
        }
        var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
        if (settings.Value.SslSettings is not null)
        {
            clientSettings.SslSettings = settings.Value.SslSettings;
            clientSettings.UseTls = true;
        }
        MongoClient client = new(clientSettings);
        Database = client.GetDatabase(settings.Value.Database ?? mongoUrl.DatabaseName);
    }

    /// <summary>
    /// 数据库
    /// </summary>
    protected IMongoDatabase Database { get; }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}