namespace EasilyNET.Test.Unit.DeepCopy.TestClass;

#nullable disable
[Serializable]
public class ComplexClass : ModerateClass
{
    public delegate void DelegateType();

    public readonly Delegate ReadonlyDelegate;
    public ISimpleClass[,,] ISimpleMultiDimArray;
    public Delegate JustDelegate;
    public Dictionary<string, SimpleClass> SampleDictionary;
    public SimpleClass[][][] SimpleMultiDimArray;
    public Struct[] StructArray;

    public ComplexClass() : base(-1, true, "fieldPrivate_" + typeof(ComplexClass))
    {
        ThisComplexClass = this;
        TupleOfThis = new(this, this, this);
        SampleDictionary = [];
        ISampleDictionary = [];
        JustDelegate = new DelegateType(() => CreateForTests());
        ReadonlyDelegate = new DelegateType(() => CreateForTests());
        JustEvent += () => CreateForTests();
    }

    public ComplexClass ThisComplexClass { get; set; }

    public Tuple<ComplexClass, ModerateClass, SimpleClass> TupleOfThis { get; protected set; }

    public Dictionary<string, ISimpleClass> ISampleDictionary { get; private set; }

    public bool IsJustEventNull => JustEvent == null;

    public event DelegateType JustEvent;

    public static ComplexClass CreateForTests()
    {
        var complexClass = new ComplexClass();
        var dict1 = new Dictionary<string, SimpleClass>();
        complexClass.SampleDictionary = dict1;
        dict1[typeof(ComplexClass).ToString()] = new ComplexClass();
        dict1[typeof(ModerateClass).ToString()] = new ModerateClass(1, true, "madeInComplexClass");
        dict1[typeof(SimpleClass).ToString()] = new(2, false, "madeInComplexClass");
        var dict2 = complexClass.ISampleDictionary;
        dict2[typeof(ComplexClass).ToString()] = dict1[typeof(ComplexClass).ToString()];
        dict2[typeof(ModerateClass).ToString()] = dict1[typeof(ModerateClass).ToString()];
        dict2[typeof(SimpleClass).ToString()] = new SimpleClass(2, false, "madeInComplexClass");
        var array1 = new ISimpleClass[2, 1, 1];
        array1[0, 0, 0] = new SimpleClass(4, false, "madeForMultiDimArray");
        array1[1, 0, 0] = new ComplexClass();
        complexClass.ISimpleMultiDimArray = array1;
        var array2 = new SimpleClass[2][][];
        array2[1] = new SimpleClass[2][];
        array2[1][1] = new SimpleClass[2];
        array2[1][1][1] = (SimpleClass)array1[0, 0, 0];
        complexClass.SimpleMultiDimArray = array2;
        complexClass.StructArray = new Struct[2];
        complexClass.StructArray[0] = new(1, complexClass, SimpleClass.CreateForTests(5));
        complexClass.StructArray[1] = new(3, new(3, false, "inStruct"), SimpleClass.CreateForTests(6));
        return complexClass;
    }
}