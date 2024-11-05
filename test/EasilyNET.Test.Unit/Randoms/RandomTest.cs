using EasilyNET.Core.Misc;

namespace EasilyNET.Test.Unit.Randoms;

[TestClass]
public class RandomTest
{
    [TestMethod]
    public void StrictNext_ShouldReturnValueWithinRange()
    {
        // Act
        var result = RandomExtensions.StrictNext();

        // Assert
        Assert.IsTrue(result is >= 0 and < int.MaxValue);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void StrictNext2_ShouldThrowArgumentOutOfRangeException_WhenStartIndexIsGreaterThanOrEqualToMaxValue()
    {
        // Act
        RandomExtensions.StrictNext(10, 5);
    }
}