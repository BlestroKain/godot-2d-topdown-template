namespace NuevoMMO.Client;

/// <summary>
/// Fases del cliente, equivalentes a GameStates de Broken Reborn.
/// El servidor sigue siendo la autoridad; esto solo describe la UI local.
/// </summary>
public enum GameFlowState : byte
{
    Disconnected,
    Menu,
    Loading,
    InWorld
}
