using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
///     <para xml:lang="en">Generic dictionary serializer for handling dictionaries with enum keys</para>
///     <para xml:lang="zh">通用的字典序列化器，用于处理使用枚举值作为键的字典类型数据</para>
///     <remarks>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterGenericSerializerDefinition(typeof(Dictionary<,>), typeof(EnumKeyDictionarySerializer<,>));
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
/// <typeparam name="TKey">
///     <para xml:lang="en">Type of the dictionary key</para>
///     <para xml:lang="zh">字典键的类型</para>
/// </typeparam>
/// <typeparam name="TValue">
///     <para xml:lang="en">Type of the dictionary value</para>
///     <para xml:lang="zh">字典值的类型</para>
/// </typeparam>
public sealed class EnumKeyDictionarySerializer<TKey, TValue> : SerializerBase<Dictionary<TKey, TValue>> where TKey : struct, Enum
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Static cache mapping enum values to their string representations to avoid repeated <see cref="Enum.ToString()"/> calls
    ///     during serialization. This trades a small amount of memory per <typeparamref name="TKey"/> for faster lookups
    ///     and reduced allocations on hot paths.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     按 <typeparamref name="TKey"/> 进行静态缓存的枚举到字符串映射，用于在序列化期间避免重复调用
    ///     <see cref="Enum.ToString()"/>。这是一个典型的“以空间换时间”的优化：为每个枚举类型占用少量额外内存，
    ///     以换取更快的查找速度和更少的临时分配。
    ///     </para>
    /// </summary>
    private static readonly Dictionary<TKey, string> _enumToString = Enum.GetValues<TKey>().ToDictionary(k => k, k => k.ToString());

    /// <summary>
    ///     <para xml:lang="en">
    ///     Static cache mapping string representations back to enum values to avoid repeated <see cref="Enum.TryParse(string, out System.Enum)"/>
    ///     (and related) calls during deserialization. This improves performance at the cost of keeping a per-<typeparamref name="TKey"/>
    ///     lookup table in memory.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     按 <typeparamref name="TKey"/> 进行静态缓存的字符串到枚举映射，用于在反序列化期间避免重复调用
    ///     <see cref="Enum.TryParse(string, out System.Enum)"/>（及类似解析方法）。通过在内存中为每个枚举类型
    ///     保留一份查找表来提升性能，同样属于“用内存换速度”的优化。
    ///     </para>
    /// </summary>
    private static readonly Dictionary<string, TKey> _stringToEnum = Enum.GetValues<TKey>().ToDictionary(k => k.ToString(), k => k);
    private readonly IBsonSerializer<TValue> _valueSerializer = BsonSerializer.LookupSerializer<TValue>();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Dictionary<TKey, TValue> value)
    {
        context.Writer.WriteStartDocument();
        foreach (var kvp in value)
        {
            // Use cached string if available, otherwise fallback to ToString() (e.g. for flags or invalid values)
            if (!_enumToString.TryGetValue(kvp.Key, out var keyStr))
            {
                keyStr = kvp.Key.ToString();
            }
            context.Writer.WriteName(keyStr);
            _valueSerializer.Serialize(context, kvp.Value);
        }
        context.Writer.WriteEndDocument();
    }

    /// <inheritdoc />
    public override Dictionary<TKey, TValue> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var dictionary = new Dictionary<TKey, TValue>();
        context.Reader.ReadStartDocument();
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var keyString = context.Reader.ReadName();
            // Use cached enum value if available
            if (_stringToEnum.TryGetValue(keyString, out var key))
            {
                var value = _valueSerializer.Deserialize(context);
                dictionary.Add(key, value);
            }
            else if (Enum.TryParse(keyString, out key))
            {
                var value = _valueSerializer.Deserialize(context);
                dictionary.Add(key, value);
            }
            else
            {
                throw new BsonSerializationException($"Cannot deserialize enum key '{keyString}' to {typeof(TKey)}.");
            }
        }
        context.Reader.ReadEndDocument();
        return dictionary;
    }
}