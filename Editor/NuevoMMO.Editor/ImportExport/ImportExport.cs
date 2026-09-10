using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public static class GameDataImportExport
{
    public static void Save(string path, ContentPackage package) => File.WriteAllText(path, package.ToJson());
    public static ContentPackage Load(string path) => ContentPackage.FromJson(File.ReadAllText(path));
}
