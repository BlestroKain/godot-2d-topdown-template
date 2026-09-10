namespace NuevoMMO.Network;

public sealed record InputFrame(long Sequence, long ClientTick, float X, float Y);
