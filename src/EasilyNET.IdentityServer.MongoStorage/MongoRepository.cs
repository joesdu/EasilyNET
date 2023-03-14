using MongoDB.Driver;
using System.Linq.Expressions;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// Mongo仓储实现
/// </summary>
public class MongoRepository : IRepository
{
    private const string prefix = "easilynet.";
    private readonly IMongoDatabase _database;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="db"></param>
    public MongoRepository(IMongoDatabase db)
    {
        _database = db;
    }

    /// <summary>
    /// 获取全部
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IQueryable<T> All<T>() where T : class, new() => _database.GetCollection<T>($"{prefix}{typeof(T).Name.ToLower()}").AsQueryable();

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
    public void Delete<T>(Expression<Func<T, bool>> expression) where T : class, new() => _database.GetCollection<T>($"{prefix}{typeof(T).Name.ToLower()}").DeleteMany(expression);

    /// <summary>
    /// 获取单条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    public T Single<T>(Expression<Func<T, bool>> expression) where T : class, new() => All<T>().Where(expression).SingleOrDefault() ?? throw new();

    /// <summary>
    /// 添加单条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    public void Add<T>(T item) where T : class, new() => _database.GetCollection<T>($"{prefix}{typeof(T).Name.ToLower()}").InsertOne(item);

    /// <summary>
    /// 批量添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    public void Add<T>(IEnumerable<T> items) where T : class, new() => _database.GetCollection<T>($"{prefix}{typeof(T).Name.ToLower()}").InsertMany(items);
}