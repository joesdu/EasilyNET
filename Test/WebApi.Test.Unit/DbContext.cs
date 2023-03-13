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