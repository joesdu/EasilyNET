using EasilyNET.Mongo.Core;
using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit;

/// <inheritdoc />
public class DbContext : MongoContext
{
    /// <summary>
    /// MongoTest
    /// </summary>
    public IMongoCollection<MongoTest> Test => GetCollection<MongoTest>("mongo.test");

    /// <summary>
    /// MongoTest2
    /// </summary>
    public IMongoCollection<MongoTest2> Test2 => GetCollection<MongoTest2>("mongo.test2");
}