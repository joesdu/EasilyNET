using EasilyNET.Core.Language;

namespace EasilyNET.Test.Unit;

/// <summary>
/// </summary>
[TestClass]
public class CustomIntEnumeratorTest
{
    /// <summary>
    /// CustomIntEnumeratorExtension Test
    /// </summary>
    /// <param name="value"></param>
    [TestMethod, DataRow(3), DataRow(5)]
    public void TestCustomIntEnumeratorExtension(int value)
    {
        foreach (var i in ..value)
        {
            Console.WriteLine(i.ToString());
        }
    }

    /// <summary>
    /// CustomIntEnumeratorExtension Test
    /// </summary>
    /// <param name="value"></param>
    [TestMethod, DataRow(3), DataRow(5)]
    public void OneToValue(int value)
    {
        foreach (var i in 1..value)
        {
            Console.WriteLine(i.ToString());
        }
    }
}