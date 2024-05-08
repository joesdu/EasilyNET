using EasilyNET.Test.Unit.DeepCopy.TestClass;

namespace EasilyNET.Test.Unit.DeepCopy;

[TestClass]
public class ModerateClassTests
{
    [TestMethod]
    public void Test1()
    {
        var m = ModerateClass.CreateForTests(1);
        var mCopy = CopyFunctionSelection.CopyMethod(m) as ModerateClass;

        // test that the copy is a different instance but with equal content
        Assert_AreEqualButNotSame(m, mCopy);

        // test of copy if we insert it as interface
        var mAsCopiedAsInterface = CopyFunctionSelection.CopyMethod(m) as ModerateClass;
        Assert_AreEqualButNotSame(m, mAsCopiedAsInterface);
    }

    public static void Assert_AreEqualButNotSame(ModerateClass? m, ModerateClass? mCopy)
    {
        if (m == null || mCopy == null)
        {
            return;
        }

        // original and copy are different instances
        Assert.AreNotSame(m, mCopy);

        // the same values in fields
        Assert.AreEqual(m.FieldPublic2, mCopy.FieldPublic2);
        Assert.AreEqual(m.PropertyPublic2, mCopy.PropertyPublic2);
        Assert.AreEqual(m.FieldPublic, mCopy.FieldPublic);
        Assert.AreEqual(m.GetPrivateField2(), mCopy.GetPrivateField2());
        Assert.AreEqual(m.GetPrivateProperty2(), mCopy.GetPrivateProperty2());
        Assert.AreEqual(m.GetPrivateField(), mCopy.GetPrivateField());
        Assert.AreEqual(m.GetPrivateProperty(), mCopy.GetPrivateProperty());
        Assert.AreEqual((string)m.ObjectTextProperty, (string)mCopy.ObjectTextProperty);

        // check that structs copied well (but with different instances of subclasses)
        Assert_StructsAreEqual(m.StructField, mCopy.StructField);

        // check that classes in struct in structs are copied well
        Assert_DeeperStructsAreEqual(m.DeeperStructField, mCopy.DeeperStructField);

        // generic classes are well copied
        Assert_GenericClassesAreEqual(m.GenericClassField, mCopy.GenericClassField);

        // subclass in property copied well
        SimpleClassTests.Assert_AreEqualButNotSame(m.SimpleClassProperty, mCopy.SimpleClassProperty);

        // subclass in readonly field copied well
        SimpleClassTests.Assert_AreEqualButNotSame(m.ReadonlySimpleClassField, mCopy.ReadonlySimpleClassField);

        // array of subclasses copied well
        if (m.SimpleClassArray == null) return;
        Assert.AreEqual(m.SimpleClassArray.Length, mCopy.SimpleClassArray.Length);
        for (var i = 0; i < m.SimpleClassArray.Length; i++)
        {
            SimpleClassTests.Assert_AreEqualButNotSame(m.SimpleClassArray[i], mCopy.SimpleClassArray[i]);
        }
    }

    private static void Assert_StructsAreEqual(Struct s, Struct sCopy)
    {
        // values are same and then are not the same
        Assert.AreEqual(s.GetItem1(), sCopy.GetItem1());
        sCopy.IncrementItem1();
        Assert.AreNotEqual(s.GetItem1(), sCopy.GetItem1());
        sCopy.DecrementItem1();

        // Item23 and Item32 in struct should be the same instance (see constructor of Struct)
        Assert.AreSame(sCopy.Item23, sCopy.Item32);

        // reference field test
        SimpleClassTests.Assert_AreEqualButNotSame(s.Item23, sCopy.Item23);

        // reference field test
        SimpleClassTests.Assert_AreEqualButNotSame(s.Item32, sCopy.Item32);

        // readonly reference field test
        SimpleClassTests.Assert_AreEqualButNotSame(s.Item4, sCopy.Item4);
    }

    private static void Assert_DeeperStructsAreEqual(DeeperStruct s, DeeperStruct sCopy)
    {
        // values are same and then are not the same
        Assert.AreEqual(s.GetItem1(), sCopy.GetItem1());
        sCopy.IncrementItem1();
        Assert.AreNotEqual(s.GetItem1(), sCopy.GetItem1());
        sCopy.DecrementItem1();

        // test that deep hidden class in structure of structs was copied well
        SimpleClassTests.Assert_AreEqualButNotSame(s.GetItem2(), sCopy.GetItem2());
    }

    private static void Assert_GenericClassesAreEqual(GenericClass<SimpleClass>? s, GenericClass<SimpleClass>? sCopy)
    {
        if (s == null || sCopy == null)
        {
            return;
        }

        // test that subclass is equal but different instance
        SimpleClassTests.Assert_AreEqualButNotSame(s.Item1, sCopy.Item1);

        // readonly reference field test
        SimpleClassTests.Assert_AreEqualButNotSame(s.Item2, sCopy.Item2);
    }
}