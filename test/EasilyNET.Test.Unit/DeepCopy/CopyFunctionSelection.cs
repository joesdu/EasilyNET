using EasilyNET.Core.DeepCopy;

namespace EasilyNET.Test.Unit.DeepCopy;

public static class CopyFunctionSelection
{
    public static readonly Func<object, object?> CopyMethod;

    static CopyFunctionSelection()
    {
        CopyMethod = obj => obj.DeepCopy();
    }
}