using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global
// ReSharper disable CommentTypo
// ReSharper disable UnusedType.Global
// ReSharper disable SuggestBaseTypeForParameter

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Ultra-fast deep copy class implemented using expression trees.</para>
///     <para xml:lang="zh">使用表达式树实现的超快速深拷贝类。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     This implementation uses compiled expression trees to minimize reflection overhead.
///     Reflection is only used during the one-time compilation phase for each type.
///     For readonly fields, FieldInfo.SetValue is used as expression trees cannot directly assign to them.
///     </para>
///     <para xml:lang="zh">
///     此实现使用编译后的表达式树来最小化反射开销。
///     反射仅在每个类型的一次性编译阶段使用。
///     对于只读字段，使用 FieldInfo.SetValue，因为表达式树无法直接对其赋值。
///     </para>
/// </remarks>
public static class DeepCopyByExpressionTrees
{
    private static readonly ConcurrentDictionary<Type, bool> IsStructTypeToDeepCopyDictionary = new();
    private static readonly ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>> CompiledCopyFunctionsDictionary = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldsCache = new();

    /// <summary>
    ///     <para xml:lang="en">Pre-cached common immutable types for fast lookup.</para>
    ///     <para xml:lang="zh">预缓存的常见不可变类型，用于快速查找。</para>
    /// </summary>
    private static readonly HashSet<Type> KnownImmutableTypes =
    [
        typeof(string), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
        typeof(Guid), typeof(decimal), typeof(Uri), typeof(Version),
        typeof(DateOnly), typeof(TimeOnly), typeof(Half), typeof(Int128), typeof(UInt128)
    ];

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
    private static readonly Type ObjectType = typeof(object);

    private static readonly Type ObjectDictionaryType = typeof(Dictionary<object, object>);
    private static readonly MethodInfo DeepCopyByExpressionTreeObjMethod = typeof(DeepCopyByExpressionTrees).GetMethod(nameof(DeepCopyObj), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo MemberwiseCloneMethod = ObjectType.GetMethod(nameof(MemberwiseClone), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly PropertyInfo DictionaryItemProperty = ObjectDictionaryType.GetProperty("Item")!;
    private static readonly MethodInfo ArrayGetLengthMethod = typeof(Array).GetMethod(nameof(Array.GetLength), BindingFlags.Public | BindingFlags.Instance)!;
    private static readonly Type FieldInfoType = typeof(FieldInfo);
    private static readonly MethodInfo SetValueMethod = FieldInfoType.GetMethod(nameof(FieldInfo.SetValue), [ObjectType, ObjectType])!;

    /// <summary>
    ///     <para xml:lang="en">Creates a deep copy of an object.</para>
    ///     <para xml:lang="zh">创建对象的深拷贝。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the object.</para>
    ///     <para xml:lang="zh">对象类型。</para>
    /// </typeparam>
    /// <param name="original">
    ///     <para xml:lang="en">The object to copy.</para>
    ///     <para xml:lang="zh">要拷贝的对象。</para>
    /// </param>
    /// <param name="copiedReferencesDict">
    ///     <para xml:lang="en">A dictionary of already copied objects (key: original object, value: their copies).</para>
    ///     <para xml:lang="zh">已拷贝对象的字典（键：原始对象，值：它们的拷贝）。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A deep copy of the original object.</para>
    ///     <para xml:lang="zh">原始对象的深拷贝。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? DeepCopy<T>(this T original, Dictionary<object, object>? copiedReferencesDict = null) => (T?)DeepCopyObj(original, false, copiedReferencesDict ?? new Dictionary<object, object>(ReferenceEqualityComparer.Instance));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? DeepCopyObj(object? original, bool forceDeepCopy, Dictionary<object, object> copiedReferencesDict)
    {
        if (original is null)
        {
            return null;
        }
        var type = original.GetType();

        // Fast path for common immutable types
        if (IsKnownImmutableType(type))
        {
            return original;
        }
        if (IsDelegate(type))
        {
            return null;
        }
        if (!forceDeepCopy && !IsTypeToDeepCopy(type))
        {
            return original;
        }
        if (copiedReferencesDict.TryGetValue(original, out var alreadyCopiedObject))
        {
            return alreadyCopiedObject;
        }
        if (type == ObjectType)
        {
            return new();
        }
        var compiledCopyFunction = GetOrCreateCompiledLambdaCopyFunction(type);
        return compiledCopyFunction(original, copiedReferencesDict);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsKnownImmutableType(Type type) => type.IsPrimitive || type.IsEnum || KnownImmutableTypes.Contains(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<object, Dictionary<object, object>, object> GetOrCreateCompiledLambdaCopyFunction(Type type) =>
        CompiledCopyFunctionsDictionary.GetOrAdd(type, static t =>
        {
            var unCompiledCopyFunction = CreateCompiledLambdaCopyFunctionForType(t);
            return unCompiledCopyFunction.Compile();
        });

    private static Expression<Func<object, Dictionary<object, object>, object>> CreateCompiledLambdaCopyFunctionForType(Type type)
    {
        InitializeExpressions(type,
            out var inputParameter,
            out var inputDictionary,
            out var outputVariable,
            out var boxingVariable,
            out var endLabel,
            out var variables,
            out var expressions);
        IfNullThenReturnNullExpression(inputParameter, endLabel, expressions);
        MemberwiseCloneInputToOutputExpression(type, inputParameter, outputVariable, expressions);
        if (IsClassOtherThanString(type))
        {
            StoreReferencesIntoDictionaryExpression(inputParameter, inputDictionary, outputVariable, expressions);
        }
        FieldsCopyExpressions(type, inputParameter, inputDictionary, outputVariable, boxingVariable, expressions);
        if (IsArray(type) && IsTypeToDeepCopy(type.GetElementType()!))
        {
            CreateArrayCopyLoopExpression(type, inputParameter, inputDictionary, outputVariable, variables, expressions);
        }
        var lambda = CombineAllIntoLambdaFunctionExpression(inputParameter, inputDictionary, outputVariable, endLabel, variables, expressions);
        return lambda;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeExpressions(Type type,
        out ParameterExpression inputParameter,
        out ParameterExpression inputDictionary,
        out ParameterExpression outputVariable,
        out ParameterExpression boxingVariable,
        out LabelTarget endLabel,
        out List<ParameterExpression> variables,
        out List<Expression> expressions)
    {
        inputParameter = Expression.Parameter(ObjectType, "input");
        inputDictionary = Expression.Parameter(ObjectDictionaryType, "dict");
        outputVariable = Expression.Variable(type, "output");
        boxingVariable = Expression.Variable(ObjectType, "boxed");
        endLabel = Expression.Label();
        variables = [outputVariable, boxingVariable];
        expressions = new(16); // Pre-allocate capacity
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void IfNullThenReturnNullExpression(ParameterExpression inputParameter, LabelTarget endLabel, List<Expression> expressions)
    {
        var ifNullThenReturnNullExpression = Expression.IfThen(Expression.ReferenceEqual(inputParameter, Expression.Constant(null, ObjectType)),
            Expression.Return(endLabel));
        expressions.Add(ifNullThenReturnNullExpression);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MemberwiseCloneInputToOutputExpression(Type type, ParameterExpression inputParameter, ParameterExpression outputVariable, List<Expression> expressions)
    {
        var memberwiseCloneInputExpression = Expression.Assign(outputVariable,
            Expression.Convert(Expression.Call(inputParameter, MemberwiseCloneMethod), type));
        expressions.Add(memberwiseCloneInputExpression);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreReferencesIntoDictionaryExpression(ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<Expression> expressions)
    {
        var storeReferencesExpression = Expression.Assign(Expression.Property(inputDictionary, DictionaryItemProperty, inputParameter),
            Expression.Convert(outputVariable, ObjectType));
        expressions.Add(storeReferencesExpression);
    }

    private static Expression<Func<object, Dictionary<object, object>, object>> CombineAllIntoLambdaFunctionExpression(
        ParameterExpression inputParameter,
        ParameterExpression inputDictionary,
        ParameterExpression outputVariable,
        LabelTarget endLabel,
        List<ParameterExpression> variables,
        List<Expression> expressions)
    {
        expressions.Add(Expression.Label(endLabel));
        expressions.Add(Expression.Convert(outputVariable, ObjectType));
        var finalBody = Expression.Block(variables, expressions);
        var lambda = Expression.Lambda<Func<object, Dictionary<object, object>, object>>(finalBody, inputParameter, inputDictionary);
        return lambda;
    }

    private static void CreateArrayCopyLoopExpression(Type type, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<ParameterExpression> variables, List<Expression> expressions)
    {
        var rank = type.GetArrayRank();
        var indices = GenerateIndices(rank);
        variables.AddRange(indices);
        var elementType = type.GetElementType()!;
        var assignExpression = ArrayFieldToArrayFieldAssignExpression(inputParameter, inputDictionary, outputVariable, elementType, type, indices);
        Expression forExpression = assignExpression;
        for (var dimension = 0; dimension < rank; dimension++)
        {
            var indexVariable = indices[dimension];
            forExpression = LoopIntoLoopExpression(inputParameter, indexVariable, forExpression, dimension);
        }
        expressions.Add(forExpression);
    }

    private static List<ParameterExpression> GenerateIndices(int arrayRank)
    {
        var indices = new List<ParameterExpression>();
        for (var i = 0; i < arrayRank; i++)
        {
            var indexVariable = Expression.Variable(typeof(int));
            indices.Add(indexVariable);
        }
        return indices;
    }

    private static BinaryExpression ArrayFieldToArrayFieldAssignExpression(ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, Type elementType, Type arrayType, List<ParameterExpression> indices)
    {
        var indexTo = Expression.ArrayAccess(outputVariable, indices);
        var indexFrom = Expression.ArrayIndex(Expression.Convert(inputParameter, arrayType), indices);
        var forceDeepCopy = elementType != ObjectType;
        var rightSide = Expression.Convert(Expression.Call(DeepCopyByExpressionTreeObjMethod,
            Expression.Convert(indexFrom, ObjectType),
            Expression.Constant(forceDeepCopy, typeof(bool)), inputDictionary), elementType);
        var assignExpression = Expression.Assign(indexTo, rightSide);
        return assignExpression;
    }

    private static BlockExpression LoopIntoLoopExpression(ParameterExpression inputParameter, ParameterExpression indexVariable, Expression loopToEncapsulate, int dimension)
    {
        var lengthVariable = Expression.Variable(typeof(int));
        var endLabelForThisLoop = Expression.Label();
        var newLoop = Expression.Loop(Expression.Block([], Expression.IfThen(Expression.GreaterThanOrEqual(indexVariable, lengthVariable),
            Expression.Break(endLabelForThisLoop)), loopToEncapsulate, Expression.PostIncrementAssign(indexVariable)), endLabelForThisLoop);
        var lengthAssignment = GetLengthForDimensionExpression(lengthVariable, inputParameter, dimension);
        var indexAssignment = Expression.Assign(indexVariable, Expression.Constant(0));
        return Expression.Block([lengthVariable], lengthAssignment, indexAssignment, newLoop);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BinaryExpression GetLengthForDimensionExpression(ParameterExpression lengthVariable, ParameterExpression inputParameter, int i)
    {
        var dimensionConstant = Expression.Constant(i);
        return Expression.Assign(lengthVariable, Expression.Call(Expression.Convert(inputParameter, typeof(Array)), ArrayGetLengthMethod, dimensionConstant));
    }

    private static void FieldsCopyExpressions(Type type, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        var fields = GetAllRelevantFields(type);
        var readonlyFields = fields.Where(f => f.IsInitOnly).ToList();
        var writableFields = fields.Where(f => !f.IsInitOnly).ToList();
        var shouldUseBoxing = readonlyFields.Count != 0;
        if (shouldUseBoxing)
        {
            var boxingExpression = Expression.Assign(boxingVariable, Expression.Convert(outputVariable, ObjectType));
            expressions.Add(boxingExpression);
        }
        foreach (var field in readonlyFields)
        {
            if (IsDelegate(field.FieldType))
            {
                ReadonlyFieldToNullExpression(field, boxingVariable, expressions);
            }
            else
            {
                ReadonlyFieldCopyExpression(type, field, inputParameter, inputDictionary, boxingVariable, expressions);
            }
        }
        if (shouldUseBoxing)
        {
            var unboxingExpression = Expression.Assign(outputVariable, Expression.Convert(boxingVariable, type));
            expressions.Add(unboxingExpression);
        }
        foreach (var field in writableFields)
        {
            if (IsDelegate(field.FieldType))
            {
                WritableFieldToNullExpression(field, outputVariable, expressions);
            }
            else
            {
                WritableFieldCopyExpression(type, field, inputParameter, inputDictionary, outputVariable, expressions);
            }
        }
    }

    private static void ReadonlyFieldToNullExpression(FieldInfo field, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        var fieldToNullExpression = Expression.Call(Expression.Constant(field), SetValueMethod, boxingVariable, Expression.Constant(null, field.FieldType));
        expressions.Add(fieldToNullExpression);
    }

    private static void ReadonlyFieldCopyExpression(Type type, FieldInfo field, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression boxingVariable, List<Expression> expressions)
    {
        var fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);
        var forceDeepCopy = field.FieldType != ObjectType;
        var fieldDeepCopyExpression = Expression.Call(Expression.Constant(field, FieldInfoType), SetValueMethod, boxingVariable,
            Expression.Call(DeepCopyByExpressionTreeObjMethod,
                Expression.Convert(fieldFrom, ObjectType),
                Expression.Constant(forceDeepCopy, typeof(bool)), inputDictionary));
        expressions.Add(fieldDeepCopyExpression);
    }

    private static void WritableFieldToNullExpression(FieldInfo field, ParameterExpression outputVariable, List<Expression> expressions)
    {
        var fieldTo = Expression.Field(outputVariable, field);
        var fieldToNullExpression = Expression.Assign(fieldTo, Expression.Constant(null, field.FieldType));
        expressions.Add(fieldToNullExpression);
    }

    private static void WritableFieldCopyExpression(Type type, FieldInfo field, ParameterExpression inputParameter, ParameterExpression inputDictionary, ParameterExpression outputVariable, List<Expression> expressions)
    {
        var fieldFrom = Expression.Field(Expression.Convert(inputParameter, type), field);
        var fieldType = field.FieldType;
        var fieldTo = Expression.Field(outputVariable, field);
        var forceDeepCopy = field.FieldType != ObjectType;
        var fieldDeepCopyExpression = Expression.Assign(fieldTo,
            Expression.Convert(Expression.Call(DeepCopyByExpressionTreeObjMethod,
                Expression.Convert(fieldFrom, ObjectType),
                Expression.Constant(forceDeepCopy, typeof(bool)), inputDictionary), fieldType));
        expressions.Add(fieldDeepCopyExpression);
    }

    private static FieldInfo[] GetAllRelevantFields(Type type, bool forceAllFields = false)
    {
        return !forceAllFields ? FieldsCache.GetOrAdd(type, static t => GetFieldsCore(t, false)) : GetFieldsCore(type, true);
    }

    private static FieldInfo[] GetFieldsCore(Type type, bool forceAllFields)
    {
        var fieldsList = new List<FieldInfo>();
        var typeCache = type;
        while (typeCache is not null)
        {
            var fields = typeCache.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            fieldsList.AddRange(fields.Where(field => forceAllFields || IsTypeToDeepCopy(field.FieldType)));
            typeCache = typeCache.BaseType;
        }
        return [.. fieldsList];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FieldInfo[] GetAllFields(Type type) => GetAllRelevantFields(type, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsArray(Type type) => type.IsArray;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeToDeepCopy(Type type) => IsClassOtherThanString(type) || IsStructWhichNeedsDeepCopy(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsClassOtherThanString(Type type) => !type.IsValueType && type != typeof(string);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsStructWhichNeedsDeepCopy(Type type) => IsStructTypeToDeepCopyDictionary.GetOrAdd(type, static t => IsStructWhichNeedsDeepCopy_NoDictionaryUsed(t));

    private static bool IsStructWhichNeedsDeepCopy_NoDictionaryUsed(Type type) => IsStructOtherThanBasicValueTypes(type) && HasInItsHierarchyFieldsWithClasses(type);

    private static bool IsStructOtherThanBasicValueTypes(Type type) => type.IsValueType && type is { IsPrimitive: false, IsEnum: false } && type != typeof(decimal);

    private static bool HasInItsHierarchyFieldsWithClasses(Type type, HashSet<Type>? alreadyCheckedTypes = null)
    {
        alreadyCheckedTypes ??= [];
        alreadyCheckedTypes.Add(type);
        var allFields = GetAllFields(type);
        var allFieldTypes = allFields.Select(f => f.FieldType).Distinct().ToList();
        var hasFieldsWithClasses = allFieldTypes.Any(IsClassOtherThanString);
        if (hasFieldsWithClasses)
        {
            return true;
        }
        var notBasicStructsTypes = allFieldTypes.Where(IsStructOtherThanBasicValueTypes).ToList();
        var typesToCheck = notBasicStructsTypes.Where(t => !alreadyCheckedTypes.Contains(t)).ToList();
        return typesToCheck.Any(typeToCheck => HasInItsHierarchyFieldsWithClasses(typeToCheck, alreadyCheckedTypes));
    }
}

/// <summary>
///     <para xml:lang="en">Reference equality comparer for object comparison.</para>
///     <para xml:lang="zh">用于对象比较的引用相等比较器。</para>
/// </summary>
file sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    /// <summary>
    ///     <para xml:lang="en">Singleton instance of the comparer.</para>
    ///     <para xml:lang="zh">比较器的单例实例。</para>
    /// </summary>
    public static readonly ReferenceEqualityComparer Instance = new();

    private ReferenceEqualityComparer() { }

    /// <inheritdoc />
    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}