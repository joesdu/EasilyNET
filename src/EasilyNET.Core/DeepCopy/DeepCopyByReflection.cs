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
/// 使用 Reflection 实现的DeepCopy,推荐用表达式树的版本
/// </summary>
public static class DeepCopyByReflection
{
    private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod("Memberwise Clone", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// 这个函数接收一个Type类型的参数，然后检查这个类型是否为string类型，或者这个类型是否为值类型并且是原始类型。如果满足这些条件中的任何一个，那么这个函数就会返回true，否则返回false。
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsPrimitive(this Type type) => type == typeof(string) || type.IsValueType & type.IsPrimitive;

    /// <summary>
    /// 使用 Reflection 实现的DeepCopy,推荐用表达式树的版本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="original"></param>
    /// <returns></returns>
    public static T? Copy<T>(this T? original) => (T?)Copy((object?)original);

    /// <summary>
    /// 使用 Reflection 实现的DeepCopy,推荐用表达式树的版本
    /// </summary>
    /// <param name="originalObject"></param>
    /// <returns></returns>
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
        if (typeToReflect.IsArray)
        {
            var arrayType = typeToReflect.GetElementType();
            if (arrayType is not null)
            {
                if (IsPrimitive(arrayType) == false)
                {
                    var clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }
            }
        }
        visited.Add(originalObject, cloneObject);
        CopyFields(originalObject, visited, cloneObject, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
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
        foreach (var fieldInfo in typeToReflect.GetFields(bindingFlags))
        {
            if (typeToReflect.Name == "Entry" && fieldInfo.Name == "value") { }
            if (filter is not null && filter(fieldInfo) == false) continue;
            if (IsPrimitive(fieldInfo.FieldType)) continue;
            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
}