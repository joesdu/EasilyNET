using EasilyNET.Core.Enums;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using MongoCRUD.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// Mongo的数组对象操作
/// </summary>
/// <param name="db"></param>
[Route("api/[controller]"), ApiController, ApiGroup("MongoTest", "v1", "MongoDB一些测试")]
public class MongoArrayController(DbContext db) : ControllerBase
{
    private readonly FilterDefinitionBuilder<FamilyInfo> _bf = Builders<FamilyInfo>.Filter;
    private readonly UpdateDefinitionBuilder<FamilyInfo> _bu = Builders<FamilyInfo>.Update;

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <returns></returns>
    [HttpPost("Init")]
    public async Task<List<FamilyInfo>> Init()
    {
        var obj = new List<FamilyInfo>
        {
            new()
            {
                Name = "野比家",
                Members =
                [
                    new()
                    {
                        Index = 0,
                        Name = "野比大助",
                        Age = 40,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1943-01-24", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 1,
                        Name = "野比玉子",
                        Age = 34,
                        Gender = EGender.女,
                        Birthday = DateOnly.ParseExact("1941-09-30", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 2,
                        Name = "野比大雄",
                        Age = 10,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1964-08-07", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 3,
                        Name = "哆啦A梦",
                        Age = 1,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("2112-09-03", "yyyy-MM-dd")
                    }
                ]
            },
            new()
            {
                Name = "蜡笔小新",
                Members =
                [
                    new()
                    {
                        Index = 0,
                        Name = "野原广志",
                        Age = 35,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1963-09-27", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 1,
                        Name = "野原美冴",
                        Age = 29,
                        Gender = EGender.女,
                        Birthday = DateOnly.ParseExact("1969-10-10", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 2,
                        Name = "野原新之助",
                        Age = 5,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1994-07-22", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 3,
                        Name = "野原向日葵",
                        Age = 1,
                        Gender = EGender.女,
                        Birthday = DateOnly.ParseExact("1998-09-27", "yyyy-MM-dd")
                    }
                ]
            },
            new()
            {
                Name = "猫和老鼠",
                Members =
                [
                    new()
                    {
                        Index = 0,
                        Name = "Tom",
                        Age = 84,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1940-02-10", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 1,
                        Name = "Jerry",
                        Age = 84,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1940-02-10", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 2,
                        Name = "Spike",
                        Age = 82,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1942-04-18", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 3,
                        Name = "Tyke",
                        Age = 70,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1953-09-03", "yyyy-MM-dd")
                    },
                    new()
                    {
                        Index = 4,
                        Name = "Nibbles",
                        Age = 77,
                        Gender = EGender.男,
                        Birthday = DateOnly.ParseExact("1946-05-08", "yyyy-MM-dd")
                    }
                ]
            }
        };
        await db.FamilyInfo.InsertManyAsync(obj);
        return obj;
    }

    /// <summary>
    /// 添加一个元素
    /// </summary>
    /// <returns></returns>
    [HttpPost("Create")]
    public async Task<Person> AddOneElement()
    {
        var dorami = new Person
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Index = 4,
            Name = "哆啦美",
            Age = 1,
            Gender = EGender.女,
            Birthday = DateOnly.ParseExact("2114-12-02", "yyyy-MM-dd")
        };
        await db.FamilyInfo.UpdateOneAsync(c => c.Name == "野比家", _bu.Push(c => c.Members, dorami));
        return dorami;
    }

    /// <summary>
    /// 更新一个元素
    /// </summary>
    /// <returns></returns>
    [HttpPut("UpdateOne")]
    public async Task UpdateOneElement()
    {
        // 这里我们举得例子是将哆啦美的名字变更为日文名字.
        // 这里我们假设查询参数同样是通过参数传入的,所以我们写出了如下代码.
        //await db.FamilyInfo.UpdateOneAsync(c => (c.Name == "野比家") & c.Members.Any(s => s.Index == 4),
        //    _bu.Set(c => c.Members.FirstMatchingElement().Name, "ドラミ"));
        //await db.FamilyInfo.UpdateOneAsync(_bf.Eq(c => c.Name, "蜡笔小新"),
        //    _bu.Inc(c => c.Members.AllMatchingElements("ele").Age, 100), new()
        //    {
        //        ArrayFilters =
        //        [
        //            //new BsonDocumentArrayFilterDefinition<BsonDocument>(new($"ele.{nameof(Person.Gender).ToLowerCamelCase()}", EGender.女.ToString()))
        //            new BsonDocumentArrayFilterDefinition<Person>(new($"ele.{nameof(Person.Age).ToLowerCamelCase()}", new BsonDocument("$lt", 1000)))
        //        ]
        //    });
        await db.FamilyInfo.UpdateOneAsync(_bf.Eq(c => c.Name, "蜡笔小新"),
            _bu.Inc(c => c.Members.AllMatchingElements("ele").Age, 100), new()
            {
                ArrayFilters =
                [
                    //new BsonDocumentArrayFilterDefinition<BsonDocument>(new($"ele.{nameof(Person.Gender).ToLowerCamelCase()}", EGender.女.ToString()))
                    new JsonArrayFilterDefinition<BsonDocument>(new($$"""
                                                                      {
                                                                        "ele.{{nameof(Person.Age).ToLowerCamelCase()}}" : {
                                                                          "$lt" : {{1000}}
                                                                        }
                                                                      }
                                                                      """))
                ]
            });
    }

    /// <summary>
    /// 删除一个元素
    /// </summary>
    /// <returns></returns>
    [HttpDelete("DeleteOne")]
    public async Task DeleteOneElement()
    {
        await db.FamilyInfo.UpdateOneAsync(c => c.Name == "野比家", _bu.PullFilter(c => c.Members, f => f.Index == 4));
    }

    /// <summary>
    /// 获取一个元素
    /// </summary>
    /// <returns></returns>
    [HttpGet("OneElement")]
    public async Task<Person> GetOneElement()
    {
        //var sql = _db.FamilyInfo.Find(c => c.Name == "野比家")
        //    .Project(c => c.Members.First()).ToString();
        return await db.FamilyInfo.Find(c => c.Name == "野比家").Project(c => c.Members.First()).SingleOrDefaultAsync();
        //return await _db.FamilyInfo.Find(c => c.Name == "野比家")
        //    .Project(c => c.Members.Find(s => s.Name == "哆啦A梦")).SingleOrDefaultAsync();
    }

    /// <summary>
    /// 获取所有元素
    /// </summary>
    /// <returns></returns>
    [HttpGet("AllElement")]
    public async Task<IEnumerable<Person>> GetAllElement()
    {
        return await db.FamilyInfo.Find(c => c.Name == "野比家").Project(c => c.Members).SingleOrDefaultAsync();
    }

    /// <summary>
    /// 对数组对象进行行列转换
    /// </summary>
    /// <returns></returns>
    [HttpGet("Unwind")]
    public async Task<dynamic> GetUnwind()
    {
        // Project中的UnwindObj我们往往使用子类,这样可以将一些不必要的数据屏蔽或者丢弃
        var query = db.FamilyInfo.Aggregate().Match(c => c.Name == "野比家")
                      .Project(c => new UnwindObj<List<Person>>
                      {
                          Obj = c.Members,
                          Count = c.Members.Count
                      })
                      .Unwind(c => c.Obj, new AggregateUnwindOptions<UnwindObj<Person>> { IncludeArrayIndex = "Index" });
        //var sql = query.ToString();
        var total = await query.Count().FirstOrDefaultAsync();
        var list = await query.ToListAsync();
        return new Tuple<long?, List<UnwindObj<Person>>>(total.Count, list);
    }
}