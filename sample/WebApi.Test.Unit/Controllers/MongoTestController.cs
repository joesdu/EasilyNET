using System.ComponentModel;
using System.Text.Json.Nodes;
using EasilyNET.Core.Attributes;
using EasilyNET.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 测试mongodb的一些功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiGroup("MongoTest", "Mongo一些测试2")]
public class MongoTestController(DbContext db) : ControllerBase
{
    private readonly FilterDefinitionBuilder<MongoTest> bf = Builders<MongoTest>.Filter;

    /// <summary>
    /// 测试JsonNode
    /// </summary>
    /// <returns></returns>
    [HttpGet("JsonNodeTest")]
    public async Task<JsonNodeTest> JsonNodeTest()
    {
        var coll = db.GetCollection<JsonNodeTest>("test.json.node");
        var id = ObjectId.GenerateNewId();
        var obj = new JsonNodeTest
        {
            Id = id.ToString(),
            JsonNode = JsonNode.Parse("""
                                      {
                                          "name": "test"
                                      }
                                      """)!
        };
        await coll.InsertOneAsync(obj);
        return await coll.Find(c => c.Id == obj.Id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// 添加一个动态数据,可便于快速测试一些代码.
    /// </summary>
    /// <returns></returns>
    [HttpPost("PostOneTest")]
    public async Task PostOneTest()
    {
        var coll = db.GetCollection<dynamic>("test.new1");
        await coll.InsertOneAsync(new
        {
            Decimal = 3.235223462346m,
            Double = 3.14d,
            Int32 = 123,
            Data = "test"
        });
    }

    /// <summary>
    /// 向MongoDB中插入.Net6+新增类型,测试自动转换是否生效
    /// </summary>
    /// <returns></returns>
    [HttpPost("MongoPost")]
    public async Task MongoPost()
    {
        var o = new MongoTest
        {
            DateTime = DateTime.Now,
            TimeSpan = TimeSpan.FromMilliseconds(50000d),
            DateOnly = DateOnly.FromDateTime(DateTime.Now),
            TimeOnly = TimeOnly.FromDateTime(DateTime.Now),
            NullableDateOnly = DateOnly.FromDateTime(DateTime.Now),
            NullableTimeOnly = null
        };
        await db.Test.InsertOneAsync(o);
    }

    /// <summary>
    /// 测试从MongoDB中取出插入的数据,再返回到Swagger查看数据JSON转换是否正常
    /// </summary>
    /// <returns></returns>
    [HttpGet("MongoGet")]
    [OutputCache]
    public async Task<IEnumerable<MongoTest>> MongoGet() => await db.Test.Find(bf.Empty).ToListAsync();

    /// <summary>
    /// 初始化Test2
    /// </summary>
    /// <returns></returns>
    [HttpPost("InitTest2")]
    public async Task InitTest2()
    {
        var os = new List<MongoTest2>();
        for (var i = 0; i < 30; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(i));
            os.Add(new() { Id = Guid.NewGuid().ToString(), Date = date, Index = i });
        }
        var session = await db.GetStartedSessionAsync();
        await db.Test2.InsertManyAsync(session, os);
        await session.CommitTransactionAsync();
    }

    /// <summary>
    /// 使用ID查询Test2
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("Test2")]
    public async Task<MongoTest2> GetTest2(string id) => await db.Test2.Find(c => c.Id == id).SingleOrDefaultAsync();

    /// <summary>
    /// 查询测试Test2
    /// </summary>
    /// <returns></returns>
    [HttpPost("Search")]
    public async Task<dynamic> Search(Search search)
    {
        var result = await db.Test2.Find(c => c.Date >= search.Start && c.Date <= search.End).ToListAsync();
        return result;
    }

    /// <summary>
    /// MultiEnum
    /// </summary>
    /// <returns></returns>
    [HttpPost("MultiEnum")]
    public async Task PostMultiEnum()
    {
        var coll = db.GetCollection<MultiEnum>(nameof(MultiEnum));
        await coll.InsertOneAsync(new());
    }
}

/// <summary>
/// 查询测试
/// </summary>
public class Search
{
    /// <summary>
    /// 开始日期
    /// </summary>
    [DefaultValue(typeof(DateOnly), "2022-11-02")]
    public DateOnly Start { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    [DefaultValue(typeof(DateOnly), "2022-11-05")]
    public DateOnly End { get; set; }

    /// <summary>
    /// 测试数值类型的参数设置默认值
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    [DefaultValue(30)]
    public int Index { get; set; }
}

file sealed class MultiEnum
{
    /// <summary>
    /// IEnumerable类型的枚举需要添加该特性,才能实现每一个元素都转成字符串,可以参考: https://github.com/joesdu/EasilyNET/issues/482
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    // ReSharper disable once UnusedMember.Local
    public EZodiac[] Zodiac { get; set; } = [EZodiac.兔, EZodiac.牛, EZodiac.狗];
}

/// <summary>
/// 测试JsonNode的序列化
/// </summary>
public sealed class JsonNodeTest
{
    /// <summary>
    /// ID
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 常规的JsonNode
    /// </summary>
    public required JsonNode JsonNode { get; set; }

    /// <summary>
    /// 可空的JsonNode
    /// </summary>
    public JsonNode? JsonNodeNullAble { get; set; }
}