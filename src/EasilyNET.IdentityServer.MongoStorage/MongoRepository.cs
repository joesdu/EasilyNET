using MongoDB.Driver;
using System.Linq.Expressions;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// Mongo仓储实现
/// </summary>
public sealed class MongoRepository : IRepository
{
    private static string pre = string.Empty;
    private readonly IMongoDatabase _database;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="db"></param>
    /// <param name="prefix">集合前缀</param>
    public MongoRepository(IMongoDatabase db, string? prefix = "")
    {
        _database = db;
        pre = prefix ?? "";
    }

    /// <summary>
    /// 获取全部
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IQueryable<T> All<T>() where T : class, new() => _database.GetCollection<T>($"{pre}{typeof(T).Name.ToLower()}").AsQueryable();

    /// <summary>
    /// Where查询表达式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    public IQueryable<T> Where<T>(Expression<Func<T, bool>> expression) where T : class, new() => All<T>().Where(expression);

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    public void Delete<T>(Expression<Func<T, bool>> expression) where T : class, new() => _database.GetCollection<T>($"{pre}{typeof(T).Name.ToLower()}").DeleteMany(expression);

    /// <summary>
    /// 获取单条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    public T Single<T>(Expression<Func<T, bool>> expression) where T : class, new() => All<T>().Where(expression).SingleOrDefault() ?? throw new("no data");

    /// <summary>
    /// 添加单条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    public void Add<T>(T item) where T : class, new() => _database.GetCollection<T>($"{pre}{typeof(T).Name.ToLower()}").InsertOne(item);

    /// <summary>
    /// 批量添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    public void Add<T>(IEnumerable<T> items) where T : class, new() => _database.GetCollection<T>($"{pre}{typeof(T).Name.ToLower()}").InsertMany(items);
}