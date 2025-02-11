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
    private readonly IBsonSerializer<TValue> _valueSerializer = BsonSerializer.LookupSerializer<TValue>();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Dictionary<TKey, TValue> value)
    {
        context.Writer.WriteStartDocument();
        foreach (var kvp in value)
        {
            context.Writer.WriteName(kvp.Key.ToString());
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
            if (Enum.TryParse(keyString, out TKey key))
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