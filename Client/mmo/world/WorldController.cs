namespace NuevoMMO.GodotClient;

public sealed class WorldController
{
    public MapLoader Loader { get; } = new();
    public ChunkManager Chunks { get; } = new();
    public WorldStreaming Streaming { get; } = new();
    public EnvironmentController Environment { get; } = new();
    public MapTransition Transitions { get; } = new();
}
