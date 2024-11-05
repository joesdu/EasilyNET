using EasilyNET.Mongo.Core;
using MongoDB.Driver;
using WebApi.Test.Unit.Domain;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit;

/// <inheritdoc />
public sealed class DbContext : MongoContext
{
    /// <summary>
    /// MongoTest
    /// </summary>
    public IMongoCollection<MongoTest> Test => GetCollection<MongoTest>("mongo.test");

    /// <summary>
    /// MongoTest2
    /// </summary>
    public IMongoCollection<MongoTest2> Test2 => GetCollection<MongoTest2>("mongo.test2");

    /// <summary>
    /// 家庭信息
    /// </summary>
    public IMongoCollection<FamilyInfo> FamilyInfo => Database.GetCollection<FamilyInfo>("family.info");
}