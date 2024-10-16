//Copied from: https://github.com/Burtsev-Alexey/net-object-deep-copy

//Source code is released under the MIT license.

//The MIT License (MIT)
// ReSharper disable once CommentTypo
//Copyright (c) 2014 Burtsev Alexey

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the "Software"), to deal in the Software without restriction, including
//without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
//sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject
//to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial
//portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT 
//NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
//THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Reflection;
using EasilyNET.Core.Misc;

// ReSharper disable UnusedType.Global
// ReSharper disable FunctionRecursiveOnAllPaths
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.DeepCopy;

/// <summary>
/// 使用 Reflection 实现的深拷贝，推荐用表达式树的版本
/// </summary>
public static class DeepCopyByReflection
{
    private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod(nameof(MemberwiseClone), BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly Dictionary<Type, FieldInfo[]> FieldInfoCache = [];

    /// <summary>
    /// 检查类型是否为原始类型或字符串
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>如果是原始类型或字符串，返回 true；否则返回 false</returns>
    private static bool IsPrimitive(this Type type) => type == typeof(string) || type is { IsValueType: true, IsPrimitive: true };

    /// <summary>
    /// 使用 Reflection 实现的深拷贝，推荐用表达式树的版本
    /// </summary>
    /// <typeparam name="T">要拷贝的对象类型</typeparam>
    /// <param name="original">要拷贝的对象</param>
    /// <returns>拷贝后的对象</returns>
    public static T? Copy<T>(this T? original) => (T?)Copy((object?)original);

    /// <summary>
    /// 使用 Reflection 实现的深拷贝，推荐用表达式树的版本
    /// </summary>
    /// <param name="originalObject">要拷贝的对象</param>
    /// <returns>拷贝后的对象</returns>
    public static object? Copy(this object? originalObject) => InternalCopy(originalObject, new Dictionary<object, object>(new ReferenceEqualityComparer()));

    private static object? InternalCopy(object? originalObject, IDictionary<object, object> visited)
    {
        if (originalObject is null) return null;
        var typeToReflect = originalObject.GetType();
        if (IsPrimitive(typeToReflect)) return originalObject;
        if (visited.TryGetValue(originalObject, out var value)) return value;
        if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
        var cloneObject = CloneMethod?.Invoke(originalObject, null);
        if (cloneObject is null) return null;
        visited.Add(originalObject, cloneObject);
        if (typeToReflect.IsArray)
        {
            var arrayType = typeToReflect.GetElementType();
            if (arrayType is null || IsPrimitive(arrayType)) return cloneObject;
            var clonedArray = (Array)cloneObject;
            clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
        }
        else
        {
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
        }
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
    {
        if (typeToReflect.BaseType is null) return;
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
        CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
    }

    private static void CopyFields(object originalObject,
        IDictionary<object, object> visited,
        object cloneObject,
        Type typeToReflect,
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
        Func<FieldInfo, bool>? filter = null)
    {
        if (!FieldInfoCache.TryGetValue(typeToReflect, out var fields))
        {
            fields = typeToReflect.GetFields(bindingFlags);
            FieldInfoCache[typeToReflect] = fields;
        }
        foreach (var fieldInfo in fields)
        {
            if (filter is not null && !filter(fieldInfo)) continue;
            if (IsPrimitive(fieldInfo.FieldType)) continue;
            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
}