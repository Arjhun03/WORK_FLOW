using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace SmartWorkflow.Api.Helpers;

public static class DataNormalizer
{
    public static Dictionary<string, object> Normalize(Dictionary<string, object>? dict)
    {
        if (dict == null) return new Dictionary<string, object>();
        var result = new Dictionary<string, object>();
        foreach (var (key, value) in dict)
        {
            result[key] = ConvertValue(value) ?? string.Empty;
        }
        return result;
    }

    public static object? ConvertValue(object? val)
    {
        if (val == null) return null;

        if (val is JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.String:
                    return elem.GetString();
                case JsonValueKind.Number:
                    if (elem.TryGetInt64(out var longVal))
                    {
                        return longVal;
                    }
                    return elem.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.Array:
                    return elem.EnumerateArray().Select(item => ConvertValue(item)).ToList();
                case JsonValueKind.Object:
                    var subDict = new Dictionary<string, object>();
                    foreach (var prop in elem.EnumerateObject())
                    {
                        subDict[prop.Name] = ConvertValue(prop.Value) ?? string.Empty;
                    }
                    return subDict;
                default:
                    return elem.ToString();
            }
        }

        if (val is Dictionary<string, object> nestedDict)
        {
            return Normalize(nestedDict);
        }

        return val;
    }
}

public class JsonElementBsonSerializer : SerializerBase<JsonElement>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonElement value)
    {
        WriteElement(context.Writer, value);
    }

    private static void WriteElement(IBsonWriter writer, JsonElement elem)
    {
        switch (elem.ValueKind)
        {
            case JsonValueKind.String:
                writer.WriteString(elem.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
                if (elem.TryGetInt64(out var l))
                {
                    writer.WriteInt64(l);
                }
                else
                {
                    writer.WriteDouble(elem.GetDouble());
                }
                break;
            case JsonValueKind.True:
                writer.WriteBoolean(true);
                break;
            case JsonValueKind.False:
                writer.WriteBoolean(false);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                writer.WriteNull();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in elem.EnumerateArray())
                {
                    WriteElement(writer, item);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Object:
                writer.WriteStartDocument();
                foreach (var prop in elem.EnumerateObject())
                {
                    writer.WriteName(prop.Name);
                    WriteElement(writer, prop.Value);
                }
                writer.WriteEndDocument();
                break;
            default:
                writer.WriteString(elem.ToString());
                break;
        }
    }

    public override JsonElement Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var bsonType = reader.GetCurrentBsonType();
        switch (bsonType)
        {
            case BsonType.String:
                var s = reader.ReadString();
                return JsonDocument.Parse(JsonSerializer.Serialize(s)).RootElement.Clone();
            case BsonType.Int32:
                var i = reader.ReadInt32();
                return JsonDocument.Parse(i.ToString()).RootElement.Clone();
            case BsonType.Int64:
                var l = reader.ReadInt64();
                return JsonDocument.Parse(l.ToString()).RootElement.Clone();
            case BsonType.Double:
                var d = reader.ReadDouble();
                return JsonDocument.Parse(d.ToString()).RootElement.Clone();
            case BsonType.Boolean:
                var b = reader.ReadBoolean();
                return JsonDocument.Parse(b ? "true" : "false").RootElement.Clone();
            case BsonType.Null:
                reader.ReadNull();
                return JsonDocument.Parse("null").RootElement.Clone();
            default:
                var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);
                return JsonDocument.Parse(doc.ToJson()).RootElement.Clone();
        }
    }
}

public static class BsonConfig
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public static void Register()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                // Register custom serializer for JsonElement
                BsonSerializer.RegisterSerializer(typeof(JsonElement), new JsonElementBsonSerializer());
            }
            catch (BsonSerializationException)
            {
                // Already registered
            }

            try
            {
                // Configure ObjectSerializer to allow JsonElement and general objects
                var objectSerializer = new ObjectSerializer(type => 
                    ObjectSerializer.DefaultAllowedTypes(type) || 
                    type == typeof(JsonElement) ||
                    type.FullName?.StartsWith("System.Text.Json") == true);

                BsonSerializer.RegisterSerializer(typeof(object), objectSerializer);
            }
            catch (BsonSerializationException)
            {
                // Already registered
            }

            _initialized = true;
        }
    }
}
