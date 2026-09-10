using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed class ContentWorkspace
{
    public ContentPackage Package { get; set; } = ContentPackage.Empty("editor");
}
