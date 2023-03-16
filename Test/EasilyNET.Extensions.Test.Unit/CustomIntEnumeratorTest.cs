using EasilyNET.Extensions.Language;

namespace EasilyNET.Extensions.Test.Unit;

/// <summary>
/// </summary>
public class CustomIntEnumeratorTest
{
    /// <summary>
    /// </summary>
    [SetUp]
    public void Setup() { }

    /// <summary>
    /// CustomIntEnumeratorExtension Test
    /// </summary>
    /// <param name="value"></param>
    [TestCase(3), TestCase(5), TestCase(0)]
    public void TestCustomIntEnumeratorExtension(int value)
    {
        foreach (var i in ..value)
        {
            Console.WriteLine(i);
        }
        Assert.Pass();
    }
}