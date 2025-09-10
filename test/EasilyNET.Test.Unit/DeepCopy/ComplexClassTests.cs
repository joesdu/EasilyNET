using EasilyNET.Test.Unit.DeepCopy.TestClass;

namespace EasilyNET.Test.Unit.DeepCopy;

[TestClass]
public class ComplexClassTests
{
    [TestMethod]
    public void Test1()
    {
        var c = ComplexClass.CreateForTests();
        var cCopy = CopyFunctionSelection.CopyMethod(c) as ComplexClass;

        // test that the copy is a different instance but with equal content
        Assert_AreEqualButNotSame(c, cCopy);

        // test that the same subobjects should remain the same in a copy (we put same objects to different dictionaries)
        Assert.AreSame(cCopy!.SampleDictionary[typeof(ComplexClass).ToString()], cCopy.ISampleDictionary[typeof(ComplexClass).ToString()]);
        Assert.AreSame(cCopy.SampleDictionary[typeof(ModerateClass).ToString()], cCopy.ISampleDictionary[typeof(ModerateClass).ToString()]);
        Assert.AreNotSame(cCopy.SampleDictionary[typeof(SimpleClass).ToString()], cCopy.ISampleDictionary[typeof(SimpleClass).ToString()]);
        Assert.AreSame(cCopy.ISimpleMultiDimArray[0, 0, 0], cCopy.SimpleMultiDimArray[1][1][1]);
    }

    private static void Assert_AreEqualButNotSame(ComplexClass? c, ComplexClass? cCopy)
    {
        if (c == null || cCopy == null)
        {
            return;
        }

        // objects are different instances
        Assert.AreNotSame(c, cCopy);

        // test on circular references
        Assert.AreSame(cCopy, cCopy.ThisComplexClass);
        Assert.AreSame(cCopy, cCopy.TupleOfThis.Item1);
        Assert.AreSame(cCopy, cCopy.TupleOfThis.Item2);
        Assert.AreSame(cCopy, cCopy.TupleOfThis.Item3);

        // original had nonnull delegates and events but copy has it null (for ExpressionTree copy method)
        Assert.IsNotNull(c.JustDelegate);
        Assert.IsNull(cCopy.JustDelegate);
        Assert.IsNotNull(c.ReadonlyDelegate);
        Assert.IsNull(cCopy.ReadonlyDelegate);
        Assert.IsFalse(c.IsJustEventNull);
        Assert.IsTrue(cCopy.IsJustEventNull);

        // test of regular dictionary
        Assert.HasCount(c.SampleDictionary.Count, cCopy.SampleDictionary);
        foreach (var pair in c.SampleDictionary.Zip(cCopy.SampleDictionary, (item, itemCopy) => new { item, itemCopy }))
        {
            Assert.AreEqual(pair.item.Key, pair.itemCopy.Key);
            Assert_AreEqualButNotSame_ChooseForType(pair.item.Value, pair.itemCopy.Value);
        }

        // test of dictionary of interfaces
        Assert.HasCount(c.ISampleDictionary.Count, cCopy.ISampleDictionary);
        foreach (var pair in c.ISampleDictionary.Zip(cCopy.ISampleDictionary, (item, itemCopy) => new { item, itemCopy }))
        {
            Assert.AreEqual(pair.item.Key, pair.itemCopy.Key);
            Assert_AreEqualButNotSame_ChooseForType(pair.item.Value, pair.itemCopy.Value);
        }

        // test of [,,] interface array
        if (c.ISimpleMultiDimArray is not null)
        {
            Assert.AreEqual(c.ISimpleMultiDimArray.Rank, cCopy.ISimpleMultiDimArray.Rank);
            for (var i = 0; i < c.ISimpleMultiDimArray.Rank; i++)
            {
                Assert.AreEqual(c.ISimpleMultiDimArray.GetLength(i), cCopy.ISimpleMultiDimArray.GetLength(i));
            }
            foreach (var pair in c.ISimpleMultiDimArray.Cast<ISimpleClass>().Zip(cCopy.ISimpleMultiDimArray.Cast<ISimpleClass>(), (item, itemCopy) => new { item, itemCopy }))
            {
                Assert_AreEqualButNotSame_ChooseForType(pair.item, pair.itemCopy);
            }
        }

        // test of array of arrays of arrays (SimpleClass[][][])
        if (c.SimpleMultiDimArray is null)
        {
            return;
        }
        Assert.HasCount(c.SimpleMultiDimArray.Length, cCopy.SimpleMultiDimArray);
        for (var i = 0; i < c.SimpleMultiDimArray.Length; i++)
        {
            var subArray = c.SimpleMultiDimArray[i];
            var subArrayCopy = cCopy.SimpleMultiDimArray[i];
            if (subArray == null)
            {
                continue;
            }
            Assert.HasCount(subArray.Length, subArrayCopy);
            for (var j = 0; j < subArray.Length; j++)
            {
                var subSubArray = subArray[j];
                var subSubArrayCopy = subArrayCopy[j];
                if (subSubArray == null)
                {
                    continue;
                }
                Assert.HasCount(subSubArray.Length, subSubArrayCopy);
                for (var k = 0; k < subSubArray.Length; k++)
                {
                    var item = subSubArray[k];
                    var itemCopy = subSubArrayCopy[k];
                    Assert_AreEqualButNotSame_ChooseForType(item, itemCopy);
                }
            }
        }
    }

    private static void Assert_AreEqualButNotSame_ChooseForType(ISimpleClass? s, ISimpleClass? sCopy)
    {
        if (s == null || sCopy == null)
        {
            return;
        }
        switch (s)
        {
            case ComplexClass complexClass:
                Assert_AreEqualButNotSame(complexClass, (ComplexClass)sCopy);
                break;
            case ModerateClass moderateClass:
                ModerateClassTests.Assert_AreEqualButNotSame(moderateClass, (ModerateClass)sCopy);
                break;
            default:
                SimpleClassTests.Assert_AreEqualButNotSame((SimpleClass)s, (SimpleClass)sCopy);
                break;
        }
    }
}