using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuevoMMO.Core;

public sealed class ContentKeyJsonConverter : JsonConverter<ContentKey>
{
    public override ContentKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new(reader.GetString() ?? throw new JsonException("ContentKey nulo."));

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("ContentKey inválido.");

        using var document = JsonDocument.ParseValue(ref reader);
        if (!document.RootElement.TryGetProperty("value", out var property))
            throw new JsonException("ContentKey.Value ausente.");
        return new(property.GetString() ?? throw new JsonException("ContentKey nulo."));
    }

    public override void Write(Utf8JsonWriter writer, ContentKey value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public sealed class DefinitionIdJsonConverter : JsonConverter<DefinitionId>
{
    public override DefinitionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return DefinitionId.Parse(reader.GetString() ?? throw new JsonException("DefinitionId nulo."));

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("DefinitionId inválido.");

        using var document = JsonDocument.ParseValue(ref reader);
        if (!document.RootElement.TryGetProperty("value", out var property))
            throw new JsonException("DefinitionId.Value ausente.");
        return DefinitionId.Parse(property.GetString() ?? throw new JsonException("DefinitionId nulo."));
    }

    public override void Write(Utf8JsonWriter writer, DefinitionId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
