namespace EasilyNET.Test.Unit.DeepCopy.TestClass;

#nullable disable
[Serializable]
public class ModerateClass(int propertyPrivate, bool propertyProtected, string fieldPrivate) : SimpleClass(propertyPrivate, propertyProtected, fieldPrivate)
{
    private readonly string FieldPrivate = fieldPrivate + "_" + typeof(ModerateClass);
    public DeeperStruct DeeperStructField;

    public int FieldPublic2;

    public GenericClass<SimpleClass> GenericClassField;

    public SimpleClass ReadonlySimpleClassField;

    public Struct StructField;

    public string PropertyPublic2 { get; set; }

    private int PropertyPrivate { get; set; } = propertyPrivate + 1;

    public SimpleClass SimpleClassProperty { get; set; }

    public SimpleClass[] SimpleClassArray { get; set; }

    public object ObjectTextProperty { get; set; } = "Test";

    public static new ModerateClass CreateForTests(int seed)
    {
        var moderateClass = new ModerateClass(seed, seed % 2 == 1, "seed_" + seed)
        {
            FieldPublic = seed,
            FieldPublic2 = seed + 1
        };
        moderateClass.StructField = new(seed, moderateClass, SimpleClass.CreateForTests(seed));
        moderateClass.DeeperStructField = new(seed, SimpleClass.CreateForTests(seed));
        moderateClass.GenericClassField = new(moderateClass, SimpleClass.CreateForTests(seed));
        var seedSimple = seed + 1000;
        moderateClass.SimpleClassProperty = new(seedSimple, seed % 2 == 1, "seed_" + seedSimple);
        moderateClass.ReadonlySimpleClassField = new(seedSimple + 1, seed % 2 == 1, "seed_" + (seedSimple + 1));
        moderateClass.SimpleClassArray = new SimpleClass[10];
        for (var i = 1; i <= 10; i++)
        {
            moderateClass.SimpleClassArray[i - 1] = new(seedSimple + i, seed % 2 == 1, "seed_" + (seedSimple + i));
        }
        return moderateClass;
    }

    public int GetPrivateProperty2() => PropertyPrivate;

    public string GetPrivateField2() => FieldPrivate;
}