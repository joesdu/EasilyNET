using EasilyNET.Test.Unit.DeepCopy.TestClass;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.DeepCopy;

[TestClass]
public class SimpleClassTests
{
    [TestMethod]
    public void Test1()
    {
        var s = SimpleClass.CreateForTests(1);
        var sCopy = CopyFunctionSelection.CopyMethod(s) as SimpleClass;

        // test that the copy is a different instance but with equal content
        Assert_AreEqualButNotSame(s, sCopy);

        // test of method "CreateForTests" that it creates different content
        var s2 = SimpleClass.CreateForTests(2);
        Assert.AreNotEqual(s.FieldPublic, s2.FieldPublic);
        Assert.AreNotEqual(s.PropertyPublic, s2.PropertyPublic);
        Assert.AreNotEqual(s.ReadOnlyField, s2.ReadOnlyField);
        Assert.AreNotEqual(s.GetPrivateField(), s2.GetPrivateField());
        Assert.AreNotEqual(s.GetPrivateProperty(), s2.GetPrivateProperty());
    }

    public static void Assert_AreEqualButNotSame(SimpleClass? s, SimpleClass? sCopy)
    {
        if (s == null || sCopy == null)
        {
            return;
        }

        // copy is different instance
        Assert.AreNotSame(s, sCopy);

        // values in properties and values are the same
        Assert.AreEqual(s.FieldPublic, sCopy.FieldPublic);
        Assert.AreEqual(s.PropertyPublic, sCopy.PropertyPublic);
        Assert.AreEqual(s.ReadOnlyField, sCopy.ReadOnlyField);
        Assert.AreEqual(s.GetPrivateField(), sCopy.GetPrivateField());
        Assert.AreEqual(s.GetPrivateProperty(), sCopy.GetPrivateProperty());

        // double check that copy is a different instance
        sCopy.FieldPublic++;
        Assert.AreNotEqual(s.FieldPublic, sCopy.FieldPublic);
        sCopy.FieldPublic--;
    }
}