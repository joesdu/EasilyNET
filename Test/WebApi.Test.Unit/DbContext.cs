using EasilyNET.Mongo;
using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit;

/// <inheritdoc />
public class DbContext : EasilyMongoContext
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
    public IMongoCollection<MongoTest> Test => GetCollection<MongoTest>("mongo.test");

    /// <summary>
    /// MongoTest2
    /// </summary>
    public IMongoCollection<MongoTest2> Test2 => GetCollection<MongoTest2>("mongo.test2");
}

/// <inheritdoc />
public class DbContext2 : EasilyMongoContext
{
    /// <summary>
    /// MongoTest
    /// </summary>
    public IMongoCollection<MongoTest> Test => GetCollection<MongoTest>("mongo.test2");
}