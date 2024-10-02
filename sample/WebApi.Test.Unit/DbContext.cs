using EasilyNET.Mongo.Core;
using MongoCRUD.Models;
using MongoDB.Driver;

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
    /// º“Õ•–≈œ¢
    /// </summary>
    public IMongoCollection<FamilyInfo> FamilyInfo => Database.GetCollection<FamilyInfo>("family.info");
}