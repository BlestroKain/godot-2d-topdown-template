using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Workspace de contenido offline. La fuente de verdad es game.db (SQLite).
/// JSON queda como fixture de tests/importación, no como archivo de edición.
/// </summary>
public sealed class ContentWorkspace
{
    private readonly DefinitionRegistry registry;
    private readonly ProjectValidator validator;
    private readonly DirtyState dirty;

    public ContentWorkspace(DefinitionRegistry registry, ProjectValidator validator, DirtyState dirty)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        this.dirty = dirty ?? throw new ArgumentNullException(nameof(dirty));
    }

    public string? CurrentPath { get; private set; }
    public string PackageVersion { get; private set; } = "editor";

    public ContentPackage Snapshot() => validator.BuildPackage(PackageVersion);

    public ContentPackage Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!GameDatabase.IsDatabasePath(fullPath))
            throw new InvalidDataException("El Editor abre GameData en SQLite (game.db), no JSON.");

        var package = GameDatabase.Load(fullPath);
        package.LoadInto(registry);
        PackageVersion = package.PackageVersion;
        CurrentPath = fullPath;
        dirty.Clear();
        return package;
    }

    public ContentPackage New(string packageVersion = "editor")
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
            throw new ArgumentException("PackageVersion requerido.", nameof(packageVersion));
        registry.Clear();
        PackageVersion = packageVersion.Trim();
        dirty.Clear();
        return Snapshot();
    }

    public void BindPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!GameDatabase.IsDatabasePath(fullPath))
            throw new InvalidDataException("El Editor guarda GameData en SQLite (game.db), no JSON.");
        CurrentPath = fullPath;
    }

    public ContentPackage Save(string? path = null)
    {
        var target = path is null ? CurrentPath : Path.GetFullPath(path);
        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("No se ha especificado una ruta para guardar el contenido.");
        if (!GameDatabase.IsDatabasePath(target))
            throw new InvalidDataException("El Editor guarda GameData en SQLite (game.db), no JSON.");

        var package = Snapshot();
        GameDatabase.Save(target, package);
        CurrentPath = target;
        dirty.Clear();
        return package;
    }

    /// <summary>
    /// Persistencia inmediata de un objeto, como el Save de Intersect (escribe esa fila en game.db).
    /// Si todavía no hay archivo, solo marca Dirty y espera Archivo → Guardar.
    /// </summary>
    public void Persist(GameDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        dirty.Mark();
        if (string.IsNullOrWhiteSpace(CurrentPath)) return;
        GameDatabase.Upsert(CurrentPath, definition);
    }

    public void SetPackageVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("PackageVersion requerido.", nameof(version));
        PackageVersion = version.Trim();
        dirty.Mark();
    }
}
