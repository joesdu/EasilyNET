using MongoDB.Driver;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core;

/// <summary>
///     <para xml:lang="en">MongoDB basic DbContext</para>
///     <para xml:lang="zh">MongoDB基础DbContext</para>
/// </summary>
public class MongoContext
{
    private int _initialized;

    /// <summary>
    ///     <see cref="IMongoClient" />
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public IMongoClient Client { get; private set; } = null!;

    /// <summary>
    ///     <para xml:lang="en">Get the specific database name configured in the connection string or MongoSettings, or the default database</para>
    ///     <para xml:lang="zh">获取链接字符串或者MongoSettings中配置的特定名称数据库或默认数据库</para>
    /// </summary>
    public IMongoDatabase Database { get; private set; } = null!;

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
    ///     <para xml:lang="en">Asynchronously start a client session, optionally with a transaction</para>
    ///     <para xml:lang="zh">异步启动客户端会话，可选择是否开启事务</para>
    /// </summary>
    /// <param name="startTransaction">
    ///     <para xml:lang="en">Whether to start a transaction immediately</para>
    ///     <para xml:lang="zh">是否立即开启事务</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    public async Task<IClientSessionHandle> StartSessionAsync(bool startTransaction = false, CancellationToken cancellationToken = default)
    {
        var session = await Client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (startTransaction)
        {
            session.StartTransaction();
        }
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

    /// <summary>
    ///     <para xml:lang="en">Initialize the <see cref="MongoContext" /> with client settings and database name</para>
    ///     <para xml:lang="zh">使用客户端配置和数据库名称初始化 <see cref="MongoContext" /></para>
    /// </summary>
    /// <param name="settings">
    ///     <see cref="MongoClientSettings" />
    /// </param>
    /// <param name="dbName">
    ///     <para xml:lang="en">Database name</para>
    ///     <para xml:lang="zh">数据库名称</para>
    /// </param>
    public void Initialize(MongoClientSettings settings, string dbName)
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
        {
            throw new InvalidOperationException("MongoContext 已经初始化，不允许重复调用 Initialize。");
        }
        Client = new MongoClient(settings);
        Database = Client.GetDatabase(dbName);
    }
}