namespace EasilyNET.Test.Unit.DeepCopy.TestClass;

[Serializable]
public class GenericClass<T>(T item1, T item2)
{
    public readonly T Item2 = item2;
    public T Item1 = item1;
}