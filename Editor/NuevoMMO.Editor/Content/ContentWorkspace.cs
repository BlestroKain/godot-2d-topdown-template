using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Workspace de contenido offline. JSON es el primer backend portable; game.db podrá implementar
/// el mismo flujo posteriormente sin contaminar Core ni la UI Godot.
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
        var package = ContentPackage.FromJson(File.ReadAllText(fullPath));
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
        CurrentPath = null;
        dirty.Clear();
        return Snapshot();
    }

    public ContentPackage Save(string? path = null)
    {
        var target = path is null ? CurrentPath : Path.GetFullPath(path);
        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("No se ha especificado una ruta para guardar el contenido.");

        var package = Snapshot();
        package.ValidateOrThrow();
        Directory.CreateDirectory(Path.GetDirectoryName(target) ?? Directory.GetCurrentDirectory());
        File.WriteAllText(target, package.ToJson());
        CurrentPath = target;
        dirty.Clear();
        return package;
    }

    public void SetPackageVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("PackageVersion requerido.", nameof(version));
        PackageVersion = version.Trim();
        dirty.Mark();
    }
}
