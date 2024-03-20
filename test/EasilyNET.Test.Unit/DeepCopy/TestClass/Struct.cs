namespace EasilyNET.Test.Unit.DeepCopy.TestClass;

[Serializable]
public struct Struct(int item1, SimpleClass item23, SimpleClass item4)
{
    private int Item1 = item1;

    public SimpleClass Item23 = item23;

    public SimpleClass Item32 = item23;

    public readonly SimpleClass Item4 = item4;

    public readonly int? GetItem1() => Item1;

    public void IncrementItem1()
    {
        Item1++;
    }

    public void DecrementItem1()
    {
        Item1--;
    }
}