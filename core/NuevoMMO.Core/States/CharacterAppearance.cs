namespace NuevoMMO.Core;

/// <summary>
/// Apariencia persistente del personaje. Las piezas son claves de contenido, nunca rutas de archivo,
/// para que el servidor pueda persistir identidad visual sin depender de Godot.
/// Los slots opcionales permanecen vacíos hasta que exista contenido gráfico real que los respalde.
/// </summary>
public sealed record CharacterAppearance
{
    public CharacterAppearance(
        ContentKey baseVisual,
        ContentKey? body = null,
        ContentKey? face = null,
        ContentKey? hair = null,
        ContentKey? eyes = null,
        ContentKey? ears = null,
        ContentKey? horns = null,
        ContentKey? pigment = null,
        ContentKey? marking = null)
    {
        if (baseVisual.IsEmpty) throw new ArgumentException("BaseVisual vacío.", nameof(baseVisual));
        ValidateOptional(body, nameof(body));
        ValidateOptional(face, nameof(face));
        ValidateOptional(hair, nameof(hair));
        ValidateOptional(eyes, nameof(eyes));
        ValidateOptional(ears, nameof(ears));
        ValidateOptional(horns, nameof(horns));
        ValidateOptional(pigment, nameof(pigment));
        ValidateOptional(marking, nameof(marking));

        BaseVisual = baseVisual;
        Body = body;
        Face = face;
        Hair = hair;
        Eyes = eyes;
        Ears = ears;
        Horns = horns;
        Pigment = pigment;
        Marking = marking;
    }

    public ContentKey BaseVisual { get; }
    public ContentKey? Body { get; }
    public ContentKey? Face { get; }
    public ContentKey? Hair { get; }
    public ContentKey? Eyes { get; }
    public ContentKey? Ears { get; }
    public ContentKey? Horns { get; }
    public ContentKey? Pigment { get; }
    public ContentKey? Marking { get; }

    public bool UsesOnlyBaseVisual => Body is null && Face is null && Hair is null && Eyes is null &&
                                      Ears is null && Horns is null && Pigment is null && Marking is null;

    /// <summary>
    /// Formato de persistencia compacto y versionable para DB. ContentKey no permite '|', por lo que
    /// el separador es inequívoco. v1 contiene BaseVisual + ocho slots semánticos.
    /// </summary>
    public string ToStorageString()
        => string.Join('|', new[]
        {
            "v1", BaseVisual.Value,
            Value(Body), Value(Face), Value(Hair), Value(Eyes), Value(Ears), Value(Horns), Value(Pigment), Value(Marking)
        });

    public static CharacterAppearance FromStorageString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return CanonicalCharacterAppearance.Default;
        var parts = value.Split('|');
        if (parts.Length != 10 || !string.Equals(parts[0], "v1", StringComparison.Ordinal))
            throw new InvalidDataException("Formato de CharacterAppearance desconocido.");

        return new CharacterAppearance(
            new ContentKey(parts[1]),
            Optional(parts[2]), Optional(parts[3]), Optional(parts[4]), Optional(parts[5]),
            Optional(parts[6]), Optional(parts[7]), Optional(parts[8]), Optional(parts[9]));
    }

    private static string Value(ContentKey? key) => key?.Value ?? string.Empty;
    private static ContentKey? Optional(string value) => string.IsNullOrEmpty(value) ? null : new ContentKey(value);

    private static void ValidateOptional(ContentKey? value, string parameter)
    {
        if (value is { } key && key.IsEmpty) throw new ArgumentException("ContentKey opcional vacío.", parameter);
    }
}

/// <summary>Único preset respaldado por arte real en esta etapa.</summary>
public static class CanonicalCharacterAppearance
{
    public static readonly ContentKey BaseVisual = new("template.player");
    public static CharacterAppearance Default => new(BaseVisual);

    public static bool IsSupported(CharacterAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        // Todavía solo existe el sprite base real. Los slots ya forman parte del contrato, pero no se
        // aceptarán desde UI/servidor hasta que el catálogo de assets los registre explícitamente.
        return appearance.BaseVisual == BaseVisual && appearance.UsesOnlyBaseVisual;
    }
}
