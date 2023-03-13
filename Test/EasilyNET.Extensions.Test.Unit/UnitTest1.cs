using EasilyNET.Extensions.Language;

namespace TestExtensions;

public class Tests
{
    [SetUp]
    public void Setup() { }

    [TestCase(4), TestCase(10), TestCase(0)]
    public void TestCustomIntEnumeratorExtension(int value)
    {
        foreach (var i in ..value)
        {
            Console.WriteLine(i);
        }
        Assert.Pass();
    }
}