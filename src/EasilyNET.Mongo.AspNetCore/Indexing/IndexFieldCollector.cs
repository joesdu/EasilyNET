using System.Collections;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;

namespace EasilyNET.Mongo.AspNetCore.Indexing;

/// <summary>
/// 索引字段收集器，负责通过反射收集实体类型上的索引特性信息
/// </summary>
internal static class IndexFieldCollector
{
    /// <summary>
    /// 递归收集类型上所有标记了索引特性的字段
    /// </summary>
    internal static void CollectIndexFields(
        Type type,
        bool useCamelCase,
        string? parentPath,
        List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> fields,
        List<string> textFields,
        List<(string Path, MongoIndexAttribute Attr)> allWildcardFields,
        HashSet<string>? timeSeriesFields = null)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var propType = prop.PropertyType;
            var fieldName = useCamelCase ? prop.Name.ToLowerCamelCase() : prop.Name;
            var fullPath = string.IsNullOrEmpty(parentPath) ? fieldName : $"{parentPath}.{fieldName}";

            // 检查是否为时序字段，如果是则跳过
            if (timeSeriesFields != null && timeSeriesFields.Contains(fieldName))
            {
                continue;
            }
            foreach (var attr in prop.GetCustomAttributes<MongoIndexAttribute>(false))
            {
                var path = fullPath;
                switch (attr.Type)
                {
                    case EIndexType.Text:
                        textFields.Add(path);
                        fields.Add((path, attr, type));
                        break;
                    case EIndexType.Wildcard:
                        {
                            // 通配符索引：支持 field.$** 格式
                            var wildcardPath = path.EndsWith("$**") ? path : $"{path}.$**";
                            allWildcardFields.Add((wildcardPath, attr));
                            break;
                        }
                    case EIndexType.Ascending:
                    case EIndexType.Descending:
                    case EIndexType.Geo2D:
                    case EIndexType.Geo2DSphere:
                    case EIndexType.Hashed:
                    case EIndexType.Multikey:
                    default:
                        {
                            // 自动检测数组或集合类型并标记为 Multikey
                            if (attr.Type == EIndexType.Multikey ||
                                propType.IsArray ||
                                (propType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string)))
                            {
                                // 为 Multikey 类型创建新的属性实例，保持原有属性设置
                                var multikeyAttr = new MongoIndexAttribute(EIndexType.Multikey)
                                {
                                    Name = attr.Name,
                                    Unique = attr.Unique,
                                    Sparse = attr.Sparse,
                                    ExpireAfterSeconds = attr.ExpireAfterSeconds,
                                    Collation = attr.Collation,
                                    TextIndexOptions = attr.TextIndexOptions
                                };
                                fields.Add((path, multikeyAttr, type));
                            }
                            else
                            {
                                fields.Add((path, attr, type));
                            }
                            break;
                        }
                }
            }

            // 递归处理嵌套对象（排除基础类型、字符串、枚举和集合类型）
            if (propType.IsClass &&
                propType != typeof(string) &&
                !propType.IsEnum &&
                !typeof(IEnumerable).IsAssignableFrom(propType) &&
                !propType.Assembly.GetName().Name!.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                CollectIndexFields(propType, useCamelCase, fullPath, fields, textFields, allWildcardFields, timeSeriesFields);
            }
        }
    }

    /// <summary>
    /// 验证文本索引
    /// </summary>
    internal static void ValidateTextIndexes(List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> allIndexFields, List<string> allTextFields)
    {
        if (allTextFields.Count <= 0)
        {
            return;
        }
        var textIndexFields = allIndexFields.Where(x => x.Attr.Type == EIndexType.Text).ToList();
        if (textIndexFields.Count > 0 && textIndexFields.Any(x => !allTextFields.Contains(x.Path)))
        {
            throw new InvalidOperationException("每个集合只允许一个文本索引，所有文本字段必须包含在同一个文本索引中。");
        }
        // 验证文本索引唯一性约束
        if (textIndexFields.Any(x => x.Attr.Unique))
        {
            throw new InvalidOperationException("文本索引不支持唯一性约束。");
        }
    }

    /// <summary>
    /// 获取嵌套属性的类型
    /// </summary>
    /// <param name="type">起始类型</param>
    /// <param name="propertyPath">属性路径，以点分隔</param>
    /// <returns>属性类型，如果未找到则返回 null</returns>
    internal static Type? GetNestedPropertyType(Type type, string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            return type;
        }
        var parts = propertyPath.Split('.');
        var currentType = type;
        foreach (var part in parts)
        {
            var property = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                return null;
            }
            currentType = property.PropertyType;

            // 处理可空类型
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;
            }
        }
        return currentType;
    }

    /// <summary>
    /// 获取时序集合的时序字段列表
    /// </summary>
    /// <param name="type">实体类型</param>
    /// <returns>时序字段名称集合</returns>
    internal static HashSet<string> GetTimeSeriesFields(Type type)
    {
        var timeSeriesFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var timeSeriesAttr = type.GetCustomAttribute<TimeSeriesCollectionAttribute>();
        // ReSharper disable once InvertIf
        if (timeSeriesAttr?.TimeSeriesOptions != null)
        {
            // 添加时间字段
            if (!string.IsNullOrWhiteSpace(timeSeriesAttr.TimeSeriesOptions.TimeField))
            {
                timeSeriesFields.Add(timeSeriesAttr.TimeSeriesOptions.TimeField);
            }

            // 添加元数据字段
            if (!string.IsNullOrWhiteSpace(timeSeriesAttr.TimeSeriesOptions.MetaField))
            {
                timeSeriesFields.Add(timeSeriesAttr.TimeSeriesOptions.MetaField);
            }
        }
        return timeSeriesFields;
    }
}