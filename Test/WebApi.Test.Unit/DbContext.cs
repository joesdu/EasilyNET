using EasilyNET.Mongo;
using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit;

/// <summary>
/// DBContext
/// </summary>
public class DbContext : EasilyNETMongoContext
{
    /// <summary>
    /// 默认无参构造函数
    /// </summary>
    public DbContext()
    {
        Console.WriteLine("DbContext无参构造函数");
    }

    /// <summary>
    /// 测试DbContext非无参构造函数
    /// </summary>
    /// <param name="test"></param>
    public DbContext(string test)
    {
        Console.WriteLine($"DbContext非无参构造函数:{test}");
    }

    /// <summary>
    /// 测试DbContext非无参构造函数
    /// </summary>
    /// <param name="test"></param>
    /// <param name="i1"></param>
    public DbContext(string test, int i1)
    {
        Console.WriteLine($"DbContext非无参构造函数:{test},{i1}");
    }

    /// <summary>
    /// MongoTest
    /// </summary>
    public IMongoCollection<MongoTest> Test => Database.GetCollection<MongoTest>("mongo.test");

    /// <summary>
    /// MongoTest2
    /// </summary>
    public IMongoCollection<MongoTest2> Test2 => Database.GetCollection<MongoTest2>("mongo.test2");
}

/// <summary>
/// DBContext2
/// </summary>
public class DbContext2 : EasilyNETMongoContext
{
    /// <summary>
    /// MongoTest
    /// </summary>
    public IMongoCollection<MongoTest> Test => Database.GetCollection<MongoTest>("mongo.test2");
}