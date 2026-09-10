using Godot;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public partial class ClientEntityView : Node2D
{
    public EntityId EntityId { get; set; }
}
