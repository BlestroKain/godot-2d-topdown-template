# Network

Protocolo, transporte y serialización. Sin gameplay.

- PacketId explícito, PacketHeader, Packet classes, DeliveryMode.
- PacketReader / PacketWriter / PacketCodec. No `var_to_bytes`.
- INetworkTransport + EnetTransport. TCP solo como adaptador de pruebas.
- Dispatch: HandlerRegistry y PacketDispatcher. Los handlers de gameplay viven en Server/Client.
