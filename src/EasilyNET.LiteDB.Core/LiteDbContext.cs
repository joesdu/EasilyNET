using LiteDB;

namespace EasilyNET.LiteDB.Core;

/// <summary>
/// LiteDbContext
/// </summary>
public class LiteDbContext : IDisposable
{
    /// <summary>
    /// Database
    /// </summary>
    private ILiteDatabase Database { get; set; } = default!;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Database is { } db)
        {
            db.Dispose();
        }
    }

    /// <summary>
    /// 获取 <see cref="ILiteCollection{TDoc}" />.
    /// </summary>
    /// <typeparam name="TDoc">实体类型</typeparam>
    /// <param name="name">集合名称</param>
    /// <returns></returns>
    public ILiteCollection<TDoc> GetCollection<TDoc>(string name) where TDoc : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return Database.GetCollection<TDoc>(name);
    }

    private static ILiteDatabase GetDatabase(string conn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conn, nameof(conn));
        return new LiteDatabase(conn, new BsonMapper());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="conn"></param>
    /// <returns></returns>
    public static T CreateInstance<T>(string conn) where T : LiteDbContext
    {
        var t = Activator.CreateInstance<T>();
        t.Database = GetDatabase(conn);
        return t;
    }
}