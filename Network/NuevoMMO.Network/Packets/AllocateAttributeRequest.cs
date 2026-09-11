using NuevoMMO.Core;

namespace NuevoMMO.Network;

/// <summary>
/// Solicitud cliente->servidor. El cliente solo indica qué atributo desea subir;
/// coste, puntos disponibles y caps se validan autoritativamente en servidor.
/// </summary>
public sealed record AllocateAttributeRequest(PrimaryAttributeId Attribute, int Increments = 1) : IPacket;
