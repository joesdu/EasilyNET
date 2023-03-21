using EasilyNET.Core.Language;
using Xunit.Abstractions;

namespace EasilyNET.Core.Test.Unit.Language;

/// <summary>
/// </summary>
public class CustomIntEnumeratorTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    /// <summary>
    /// CustomIntEnumeratorTest
    /// </summary>
    /// <param name="testOutputHelper"></param>
    public CustomIntEnumeratorTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// CustomIntEnumeratorExtension Test
    /// </summary>
    /// <param name="value"></param>
    [Theory, InlineData(3)]
    public void TestCustomIntEnumeratorExtension(int value)
    {
        foreach (var i in ..value)
        {
            _testOutputHelper.WriteLine(i.ToString());
        }
    }
}