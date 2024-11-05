namespace EasilyNET.Test.Unit.DeepCopy.TestClass;

[Serializable]
public struct DeeperStruct(int item1, SimpleClass item2)
{
    [Serializable]
    private struct SubStruct
    {
        public int Item1;

        public SimpleClass Item2;
    }

    private SubStruct SubStructItem = new() { Item1 = item1, Item2 = item2 };

    public readonly int GetItem1() => SubStructItem.Item1;

    public void IncrementItem1()
    {
        SubStructItem.Item1++;
    }

    public void DecrementItem1()
    {
        SubStructItem.Item1--;
    }

    public readonly SimpleClass GetItem2() => SubStructItem.Item2;
}