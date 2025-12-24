using EasilyNET.Core.Misc;

namespace EasilyNET.Test.Unit.Randoms;

[TestClass]
public class RandomTest
{
    [TestMethod]
    public void StrictNext_ShouldReturnValueWithinRange()
    {
        // Act
        var result = Random.StrictNext();

        // Assert
        Assert.IsTrue(result is >= 0 and < int.MaxValue);
    }

    [TestMethod]
    public void StrictNext2_ShouldThrowArgumentOutOfRangeException_WhenStartIndexIsGreaterThanOrEqualToMaxValue()
    {
        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => Random.StrictNext(10, 5));
    }
}