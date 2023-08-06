using  EasilyNET.Core.Data;
namespace EasilyNET.Test.Unit.Data;


/// <summary>
/// 测试
/// </summary>
[TestClass]
public class MaybeTest
{
    
    [TestMethod]
    public void TestHasValue()
    {
        Maybe<string> maybeValue = null;

        Assert.IsTrue(maybeValue.HasValue);
    }
    

    [TestMethod]
    public void TestValueThrowsExceptionWhenHasValue()
    {
        Maybe<string> maybeNull = null;

        
        Assert.ThrowsException<InvalidOperationException>(()=>maybeNull.Value);
    }
    
}