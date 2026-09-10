namespace NuevoMMO.Editor;

public sealed class EditorConfiguration
{
    public EditorMode Mode { get; init; } = EditorMode.Offline;
    public string ContentPath { get; init; } = "gamedata";

    /// <summary>
    /// Ruta relativa o absoluta a los recursos visuales del cliente. El Editor la resuelve
    /// desde el directorio de trabajo y, si hace falta, buscando la raíz del repositorio.
    /// </summary>
    public string ClientAssetRoot { get; init; } = "Client";

    /// <summary>
    /// Carpeta de imágenes de tilesets dentro de ClientAssetRoot.
    /// </summary>
    public string TilesetAssetFolder { get; init; } = "tilesets";
}
