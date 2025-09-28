using MongoDB.Driver;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core;

#nullable disable // Disable nullable check for a response from the community

/// <summary>
///     <para xml:lang="en">MongoDB basic DbContext</para>
///     <para xml:lang="zh">MongoDB基础DbContext</para>
/// </summary>
public class MongoContext : IDisposable
{
    /// <summary>
    ///     <see cref="IMongoClient" />
    /// </summary>
    public IMongoClient Client { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Get the specific database name configured in the connection string or MongoSettings, or the default database</para>
    ///     <para xml:lang="zh">获取链接字符串或者MongoSettings中配置的特定名称数据库或默认数据库</para>
    /// </summary>
    public IMongoDatabase Database { get; private set; }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Get <see cref="IMongoCollection{TDocument}" />.</para>
    ///     <para xml:lang="zh">获取 <see cref="IMongoCollection{TDocument}" />.</para>
    /// </summary>
    /// <typeparam name="TDocument">
    ///     <para xml:lang="en">Entity</para>
    ///     <para xml:lang="zh">实体</para>
    /// </typeparam>
    /// <param name="name">
    ///     <para xml:lang="en">Collection name</para>
    ///     <para xml:lang="zh">集合名称</para>
    /// </param>
    public IMongoCollection<TDocument> GetCollection<TDocument>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Database.GetCollection<TDocument>(name);
    }

    /// <summary>
    ///     <para xml:lang="en">Synchronously get a started <see cref="IClientSessionHandle">Session</see> with a transaction</para>
    ///     <para xml:lang="zh">同步方式获取一个已开启事务的 <see cref="IClientSessionHandle">Session</see></para>
    /// </summary>
    public IClientSessionHandle GetStartedSession()
    {
        var session = Client.StartSession();
        session.StartTransaction();
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously get a started <see cref="IClientSessionHandle">Session</see> with a transaction</para>
    ///     <para xml:lang="zh">异步方式获取一个已开启事务的 <see cref="IClientSessionHandle">Session</see></para>
    /// </summary>
    public async Task<IClientSessionHandle> GetStartedSessionAsync()
    {
        var session = await Client.StartSessionAsync();
        session.StartTransaction();
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Create an instance of the <see cref="MongoContext" /> subclass</para>
    ///     <para xml:lang="zh">创建 <see cref="MongoContext" /> 子类实例</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">DbContext</para>
    ///     <para xml:lang="zh">DbContext</para>
    /// </typeparam>
    /// <param name="settings">
    ///     <see cref="MongoClientSettings" />
    /// </param>
    /// <param name="dbName">
    ///     <para xml:lang="en">Database name</para>
    ///     <para xml:lang="zh">数据库名称</para>
    /// </param>
    public static T CreateInstance<T>(MongoClientSettings settings, string dbName) where T : MongoContext
    {
        var t = Activator.CreateInstance<T>();
        t.Client = new MongoClient(settings);
        t.Database = t.Client.GetDatabase(dbName);
        return t;
    }
}