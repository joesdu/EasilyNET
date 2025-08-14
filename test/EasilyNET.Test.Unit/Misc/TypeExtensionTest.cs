using EasilyNET.Core.Misc;

namespace EasilyNET.Test.Unit.Misc;

[TestClass]
public class TypeExtensionTest
{
    [TestMethod]
    [DataRow(typeof(int), "Int32")]
    [DataRow(typeof(string), "String")]
    [DataRow(typeof(List<int>), "List<Int32>")]
    [DataRow(typeof(Dictionary<string, List<int>>), "Dictionary<String, List<Int32>>")]
    [DataRow(typeof(Dictionary<string, List<int?>>), "Dictionary<String, List<Nullable<Int32>>>")]
    public void GetFriendlyTypeName_ShouldReturnExpectedResult(Type type, string expected)
    {
        // Act
        var result = type.GetFriendlyTypeName();

        // Assert
        Assert.AreEqual(expected, result);
    }
}