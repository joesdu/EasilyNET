using EasilyNET.Core.Domains;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Entities;

/// <summary>
/// 领域实体测试
/// </summary>
[TestClass]
public class EntityTest
{
    
    /// <summary>
    /// 测试是否相等
    /// </summary>
    [TestMethod]
    public void TestOrderEntityWhenTrue()
    {
        var order1 = new Order(10)
        {
            
            Name = "大黄瓜18CM",
            Status = 0,
            Price=10
        };
        var order2 = new Order(10)
        {

            Name = "大黄瓜18CM",
            Status = 0,
            Price = 10
        };

        order1.Equals(order2).Should().BeTrue();
        
        var order3 = new Order(10)
        {

            Name = "大黄瓜180CM",
            Status = 0,
            Price = 30
        };

        order2.Equals(order3).Should().BeTrue();
    }
    
       
    /// <summary>
    /// 测试是否不相等
    /// </summary>
    [TestMethod]
    public void TestOrderEntityNotTrue()
    {
        var order1 = new Order(12)
        {
            
            Name = "大黄瓜18CM",
            Status = 0,
            Price=10
        };
        var order2 = new Order(10)
        {

            Name = "大黄瓜18CM",
            Status = 0,
            Price = 10
        };

        order1.Equals(order2).Should().BeFalse();
        
 
    }
    
}

/// <summary>
/// 
/// </summary>
public sealed class Order : Entity<OrderId>
{


    public Order(OrderId id)
    {
        Id = id;

    }


    public string Name { get; set; }

    public decimal Price { get; set; }
    
    public int Status{ get; set; }


}


/// <summary>
/// 主键ID
/// </summary>
/// <param name="Id"></param>
public record OrderId(long Id)
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator long(OrderId id) => id.Id;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator OrderId(long id) => new OrderId(id);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Id.ToString();
    }
}