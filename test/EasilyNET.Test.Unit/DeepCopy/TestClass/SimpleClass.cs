namespace EasilyNET.Test.Unit.DeepCopy.TestClass;

#nullable disable
[Serializable]
public class SimpleClass : ISimpleClass
{
    private readonly string FieldPrivate;
    public readonly string ReadOnlyField;
    public int FieldPublic;

    public SimpleClass(int propertyPrivate, bool propertyProtected, string fieldPrivate)
    {
        PropertyPrivate = propertyPrivate;
        PropertyProtected = propertyProtected;
        FieldPrivate = fieldPrivate + "_" + typeof(SimpleClass);
        ReadOnlyField = FieldPrivate + "_readonly";
    }

    protected bool PropertyProtected { get; set; }

    private int PropertyPrivate { get; set; }

    public string PropertyPublic { get; set; } = string.Empty;

    public static SimpleClass CreateForTests(int seed) =>
        new(seed, seed % 2 == 1, "seed_" + seed)
        {
            FieldPublic = -seed,
            PropertyPublic = "seed_" + seed + "_public"
        };

    public int GetPrivateProperty() => PropertyPrivate;
    public string GetPrivateField() => FieldPrivate;
}