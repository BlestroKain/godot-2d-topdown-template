using NuevoMMO.Core;

namespace NuevoMMO.Editor;

/// <summary>
/// Resolución de archivos en la carpeta compartida <c>Client/resources</c>.
/// Editor, cliente C# y Godot (<c>res://resources</c>) leen el mismo árbol.
/// </summary>
public sealed class AssetLibrary
{
    private static readonly string[] ImageExtensions = [".png", ".webp", ".jpg", ".jpeg", ".bmp"];
    private static readonly string[] SoundExtensions = [".wav", ".ogg", ".mp3"];

    public AssetLibrary(EditorConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Root = LocateSharedRoot(configuration.ResourcesRoot);
    }

    public string Root { get; }

    public void EnsureLayout()
    {
        Directory.CreateDirectory(Root);
        foreach (var kind in AssetCatalog.AllKinds)
            Directory.CreateDirectory(Path.Combine(Root, AssetCatalog.Folder(kind)));
    }

    public string Folder(AssetKind kind)
    {
        var path = Path.Combine(Root, AssetCatalog.Folder(kind));
        Directory.CreateDirectory(path);
        return path;
    }

    public IReadOnlyList<string> ListFileNames(AssetKind kind)
    {
        var folder = Folder(kind);
        var extensions = kind is AssetKind.Sound or AssetKind.Music ? SoundExtensions : ImageExtensions;
        return Directory.EnumerateFiles(folder)
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string Resolve(AssetKind kind, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var folder = Folder(kind);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var direct = Path.Combine(folder, fileName);
        if (File.Exists(direct)) return Path.GetFullPath(direct);

        var extensions = kind is AssetKind.Sound or AssetKind.Music ? SoundExtensions : ImageExtensions;
        foreach (var extension in extensions)
        {
            var candidate = Path.Combine(folder, stem + extension);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }

        throw new FileNotFoundException($"No se encontró '{fileName}' en '{folder}'.");
    }

    public bool TryResolve(ContentKey key, out string path)
    {
        path = string.Empty;
        if (key.IsEmpty || !AssetCatalog.TryKind(key, out var kind)) return false;
        try
        {
            path = Resolve(kind, AssetCatalog.FileStem(key));
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public ContentKey Key(AssetKind kind, string fileName) => AssetCatalog.Key(kind, fileName);

    public static string LocateSharedRoot(string configured)
    {
        if (Path.IsPathRooted(configured) && Directory.Exists(configured))
            return Path.GetFullPath(configured);

        foreach (var origin in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(origin);
            while (current is not null)
            {
                foreach (var relative in DistinctRoots(configured))
                {
                    var candidate = Path.Combine(current.FullName, relative);
                    if (Directory.Exists(candidate)) return Path.GetFullPath(candidate);
                }

                current = current.Parent;
            }
        }

        var created = Path.GetFullPath(Path.Combine(FindRepoRoot(), AssetCatalog.SharedRootFromRepo));
        Directory.CreateDirectory(created);
        return created;
    }

    private static IEnumerable<string> DistinctRoots(string configured)
    {
        yield return configured.Replace('/', Path.DirectorySeparatorChar);
        yield return AssetCatalog.SharedRootFromRepo.Replace('/', Path.DirectorySeparatorChar);
        yield return Path.Combine("Client", AssetCatalog.DefaultRoot);
        yield return AssetCatalog.DefaultRoot;
    }

    private static string FindRepoRoot()
    {
        foreach (var origin in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(origin);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "NuevoMMO.sln")) ||
                    Directory.Exists(Path.Combine(current.FullName, "Client")))
                    return current.FullName;
                current = current.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
