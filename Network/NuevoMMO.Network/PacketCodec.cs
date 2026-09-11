using System.Buffers.Binary;
using NuevoMMO.Core;

namespace NuevoMMO.Network;

public static class PacketCodec
{
    public const ushort Version = 2;
    public const int MaxPacketBytes = 64 * 1024;
    public const int MaxEntities = 256;
    private static ReadOnlySpan<byte> Magic => "NMMO"u8;

    public static byte[] Encode(IPacket packet)
    {
        var descriptor = PacketRegistry.Describe(packet);
        using var payloadWriter = new PacketWriter();
        WritePayload(payloadWriter, packet);
        var payload = payloadWriter.ToArray();
        if (payload.Length > MaxPacketBytes - PacketHeader.Size) throw new InvalidDataException("Paquete demasiado grande.");
        var output = new byte[PacketHeader.Size + payload.Length];
        Magic.CopyTo(output);
        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(4), Version);
        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(6), (ushort)descriptor.Id);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(8), (uint)payload.Length);
        payload.CopyTo(output, PacketHeader.Size);
        return output;
    }

    public static IPacket Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < PacketHeader.Size || data.Length > MaxPacketBytes || !data[..4].SequenceEqual(Magic))
            throw new InvalidDataException("Cabecera NMMO inválida.");
        var version = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        if (version != Version) throw new InvalidDataException("Versión de protocolo incompatible.");
        var id = (PacketId)BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        if (payloadLength != data.Length - PacketHeader.Size) throw new InvalidDataException("Longitud de payload inválida.");
        try
        {
            using var reader = new PacketReader(data[PacketHeader.Size..]);
            var packet = ReadPayload(reader, id);
            reader.EnsureComplete();
            return packet;
        }
        catch (InvalidDataException) { throw; }
        catch (Exception exception) when (exception is EndOfStreamException or ArgumentException or OverflowException)
        {
            throw new InvalidDataException("Paquete truncado o mal formado.", exception);
        }
    }

    public static PacketHeader ReadHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < PacketHeader.Size || !data[..4].SequenceEqual(Magic)) throw new InvalidDataException("Cabecera NMMO inválida.");
        return new(BinaryPrimitives.ReadUInt16LittleEndian(data[4..]),
            (PacketId)BinaryPrimitives.ReadUInt16LittleEndian(data[6..]), BinaryPrimitives.ReadUInt32LittleEndian(data[8..]));
    }

    private static void WritePayload(PacketWriter writer, IPacket packet)
    {
        switch (packet)
        {
            case ConnectRequest value: writer.Write(value.ClientVersion, 64); writer.Write(value.ProtocolVersion); break;
            case LoginRequest value: writer.Write(value.Username, 128); writer.Write(value.Password, 256); break;
            case RegisterRequest value: writer.Write(value.Username, 128); writer.Write(value.Password, 256); break;
            case CharacterListRequest value: WriteSession(writer, value.Session, value.SessionToken); break;
            case CreateCharacterRequest value: WriteSession(writer, value.Session, value.SessionToken); writer.Write(value.Name, 128); break;
            case CharacterSelectRequest value: WriteSession(writer, value.Session, value.SessionToken); writer.Write(value.Character.Value); break;
            case MapReadyRequest value: writer.Write(value.Instance.Value); break;
            case MoveRequest value: writer.Write(value.Input.Sequence); writer.Write(value.Input.ClientTick); writer.Write(value.Input.X); writer.Write(value.Input.Y); break;
            case AllocateAttributeRequest value:
                if (!Enum.IsDefined(value.Attribute) || value.Increments is < 1 or > 100)
                    throw new InvalidDataException("Solicitud de atributo inválida.");
                writer.Write((byte)value.Attribute); writer.Write(value.Increments); break;
            case DevelopmentAttackRequest value:
                if (value.Target.Value <= 0 || !Enum.IsDefined(value.Attack))
                    throw new InvalidDataException("Ataque de desarrollo inválido.");
                writer.Write(value.Target.Value); writer.Write((byte)value.Attack); break;
            case BasicAttackRequest value:
                if (value.Target.Value <= 0) throw new InvalidDataException("BasicAttack inválido.");
                writer.Write(value.Target.Value); break;
            case UseTechniqueRequest value:
                if (value.TechniqueId.IsEmpty) throw new InvalidDataException("UseTechnique inválido.");
                writer.Write(value.TechniqueId.Value); writer.Write(value.Target.Value); writer.Write(value.Point); break;
            case InteractRequest value:
                writer.Write(value.Target.Value); break;
            case SetTargetRequest value:
                writer.Write(value.Target.Value); break;
            case PingPacket value: writer.Write(value.Nonce); writer.Write(value.ClientSendTimestamp); break;
            case DisconnectRequest value: writer.Write(value.Reason, 256); break;
            case ConnectionAccepted value: writer.Write(value.Connection.Value); writer.Write(value.ServerTimestamp); writer.Write(value.ProtocolVersion); break;
            case LoginResult value:
                writer.Write(value.Succeeded); writer.Write(value.Error, 256); writer.Write(value.Account.Value);
                writer.Write(value.Session.Value); writer.Write(value.SessionToken, 256); break;
            case RegisterResult value: writer.Write(value.Succeeded); writer.Write(value.Error, 256); writer.Write(value.Account.Value); break;
            case CharacterListResult value: WriteCount(writer, value.Characters.Length, 64); foreach (var character in value.Characters) WriteCharacter(writer, character); break;
            case CharacterCreated value: WriteCharacter(writer, value.Character); break;
            case CharacterSelected value: writer.Write(value.Character.Value); break;
            case MapLoadPacket value: WriteMap(writer, value.Map); writer.Write(value.Self.Value); writer.Write(value.Character.Value); break;
            case SpawnEntityPacket value: WriteEntity(writer, value.Entity); break;
            case DespawnEntityPacket value: writer.Write(value.Entity.Value); break;
            case EntityMovedPacket value: writer.Write(value.Entity.Value); writer.Write(value.Position); writer.Write(value.Velocity); writer.Write(value.ServerTick); break;
            case EntityStatePacket value:
                writer.Write(value.Tick); writer.Write(value.Full); writer.Write(value.Correction.Self.Value);
                writer.Write(value.Correction.Position); writer.Write(value.Correction.LastProcessedInput);
                WriteCount(writer, value.Upserts.Length, MaxEntities); foreach (var entity in value.Upserts) WriteEntity(writer, entity);
                WriteCount(writer, value.Despawns.Length, MaxEntities); foreach (var entity in value.Despawns) writer.Write(entity.Value);
                break;
            case PlayerStatsPacket value: WritePlayerStats(writer, value.Stats); break;
            case CombatDebugPacket value: WriteCombatDebug(writer, value); break;
            case ServerTimePacket value: writer.Write(value.ServerTimestamp); writer.Write(value.ServerTick); break;
            case PongPacket value: writer.Write(value.Nonce); writer.Write(value.ClientSendTimestamp); writer.Write(value.ServerReceiveTimestamp); writer.Write(value.ServerSendTimestamp); break;
            case ErrorPacket value: writer.Write(value.Code, 64); writer.Write(value.Message, 512); writer.Write(value.Fatal); break;
            default: throw new InvalidDataException("Tipo de paquete no permitido.");
        }
    }

    private static IPacket ReadPayload(PacketReader reader, PacketId id) => id switch
    {
        PacketId.ConnectRequest => new ConnectRequest(reader.ReadString(64), reader.ReadUInt16()),
        PacketId.LoginRequest => new LoginRequest(reader.ReadString(128), reader.ReadString(256)),
        PacketId.RegisterRequest => new RegisterRequest(reader.ReadString(128), reader.ReadString(256)),
        PacketId.CharacterListRequest => new CharacterListRequest(new(reader.ReadGuid()), reader.ReadString(256)),
        PacketId.CreateCharacterRequest => new CreateCharacterRequest(new(reader.ReadGuid()), reader.ReadString(256), reader.ReadString(128)),
        PacketId.CharacterSelectRequest => new CharacterSelectRequest(new(reader.ReadGuid()), reader.ReadString(256), new(reader.ReadGuid())),
        PacketId.MapReadyRequest => new MapReadyRequest(new(reader.ReadInt64())),
        PacketId.MoveRequest => new MoveRequest(new(reader.ReadInt64(), reader.ReadInt64(), reader.ReadSingle(), reader.ReadSingle())),
        PacketId.AllocateAttributeRequest => ReadAllocateAttributeRequest(reader),
        PacketId.DevelopmentAttackRequest => ReadDevelopmentAttackRequest(reader),
        PacketId.BasicAttackRequest => new BasicAttackRequest(new(reader.ReadInt64())),
        PacketId.UseTechniqueRequest => new UseTechniqueRequest(new(reader.ReadGuid()), new(reader.ReadInt64()), reader.ReadVector2()),
        PacketId.InteractRequest => new InteractRequest(new(reader.ReadInt64())),
        PacketId.SetTargetRequest => new SetTargetRequest(new(reader.ReadInt64())),
        PacketId.Ping => new PingPacket(reader.ReadInt64(), reader.ReadInt64()),
        PacketId.DisconnectRequest => new DisconnectRequest(reader.ReadString(256)),
        PacketId.ConnectionAccepted => new ConnectionAccepted(new(reader.ReadGuid()), reader.ReadInt64(), reader.ReadUInt16()),
        PacketId.LoginResult => new LoginResult(reader.ReadBool(), reader.ReadString(256), new(reader.ReadGuid()), new(reader.ReadGuid()), reader.ReadString(256)),
        PacketId.RegisterResult => new RegisterResult(reader.ReadBool(), reader.ReadString(256), new(reader.ReadGuid())),
        PacketId.CharacterListResult => new CharacterListResult(ReadArray(reader, 64, () => ReadCharacter(reader))),
        PacketId.CharacterCreated => new CharacterCreated(ReadCharacter(reader)),
        PacketId.CharacterSelected => new CharacterSelected(new(reader.ReadGuid())),
        PacketId.MapLoad => new MapLoadPacket(ReadMap(reader), new(reader.ReadInt64()), new(reader.ReadGuid())),
        PacketId.SpawnEntity => new SpawnEntityPacket(ReadEntity(reader)),
        PacketId.DespawnEntity => new DespawnEntityPacket(new(reader.ReadInt64())),
        PacketId.EntityMoved => new EntityMovedPacket(new(reader.ReadInt64()), reader.ReadVector2(), reader.ReadVector2(), reader.ReadInt64()),
        PacketId.EntityState => ReadEntityStatePacket(reader),
        PacketId.PlayerStats => new PlayerStatsPacket(ReadPlayerStats(reader)),
        PacketId.CombatDebug => ReadCombatDebug(reader),
        PacketId.ServerTime => new ServerTimePacket(reader.ReadInt64(), reader.ReadInt64()),
        PacketId.Pong => new PongPacket(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64()),
        PacketId.Error => new ErrorPacket(reader.ReadString(64), reader.ReadString(512), reader.ReadBool()),
        _ => throw new InvalidDataException("PacketId desconocido.")
    };

    private static AllocateAttributeRequest ReadAllocateAttributeRequest(PacketReader reader)
    {
        var attribute = (PrimaryAttributeId)reader.ReadByte();
        var increments = reader.ReadInt32();
        if (!Enum.IsDefined(attribute) || increments is < 1 or > 100)
            throw new InvalidDataException("Solicitud de atributo inválida.");
        return new(attribute, increments);
    }

    private static DevelopmentAttackRequest ReadDevelopmentAttackRequest(PacketReader reader)
    {
        var target = new EntityId(reader.ReadInt64());
        var attack = (DevelopmentAttackKind)reader.ReadByte();
        if (target.Value <= 0 || !Enum.IsDefined(attack))
            throw new InvalidDataException("Ataque de desarrollo inválido.");
        return new(target, attack);
    }

    private static EntityStatePacket ReadEntityStatePacket(PacketReader reader)
    {
        var tick = reader.ReadInt64(); var full = reader.ReadBool();
        var correction = new MovementCorrection(new(reader.ReadInt64()), reader.ReadVector2(), reader.ReadInt64());
        var upserts = ReadArray(reader, MaxEntities, () => ReadEntity(reader));
        var despawns = ReadArray(reader, MaxEntities, () => new EntityId(reader.ReadInt64()));
        if (tick < 0 || correction.Self.Value <= 0 || correction.LastProcessedInput < 0) throw new InvalidDataException("Estado autoritativo inválido.");
        return new(tick, full, correction, upserts, despawns);
    }

    private static void WriteSession(PacketWriter writer, SessionId session, string token) { writer.Write(session.Value); writer.Write(token, 256); }
    private static void WriteCharacter(PacketWriter writer, CharacterSummary character)
    { writer.Write(character.Id.Value); writer.Write(character.Name, 128); writer.Write(character.MapDefinition.Value); writer.Write(character.Position); }
    private static CharacterSummary ReadCharacter(PacketReader reader) => new(new(reader.ReadGuid()), reader.ReadString(128), new(reader.ReadGuid()), reader.ReadVector2());
    private static void WriteMap(PacketWriter writer, MapProjection map)
    {
        writer.Write(map.Definition.Value); writer.Write(map.Instance.Value); writer.Write(map.VisualKey);
        writer.Write(map.Bounds.Minimum); writer.Write(map.Bounds.Maximum); writer.Write(map.MovementSpeed);
        writer.Write(map.TickMilliseconds); writer.Write(map.ContentVersion, 64);
    }
    private static MapProjection ReadMap(PacketReader reader) => new(new(reader.ReadGuid()), new(reader.ReadInt64()), reader.ReadContentKey(),
        new(reader.ReadVector2(), reader.ReadVector2()), reader.ReadSingle(), reader.ReadInt32(), reader.ReadString(64));

    private static void WriteAttribute(PacketWriter writer, AttributeSnapshot value)
    {
        writer.Write(value.Natural); writer.Write(value.Effective); writer.Write(value.NextCost);
    }

    private static AttributeSnapshot ReadAttribute(PacketReader reader)
    {
        var value = new AttributeSnapshot(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        if (value.Natural < 0 || value.Effective < 0 || value.NextCost < 0)
            throw new InvalidDataException("Atributo de jugador inválido.");
        return value;
    }

    private static void WritePlayerStats(PacketWriter writer, PlayerStatsSnapshot value)
    {
        writer.Write(value.Level); writer.Write(value.Experience); writer.Write(value.ExperienceToNextLevel); writer.Write(value.AvailableAttributePoints);
        WriteAttribute(writer, value.Strength); WriteAttribute(writer, value.Intelligence); WriteAttribute(writer, value.Agility);
        WriteAttribute(writer, value.Spirit); WriteAttribute(writer, value.Vitality);
        writer.Write(value.Health); writer.Write(value.MaxHealth); writer.Write(value.Mana); writer.Write(value.MaxMana);
        writer.Write(value.Defense); writer.Write(value.ManaRegenPerSecond); writer.Write(value.OutOfCombatManaRegenPerSecond);
        writer.Write(value.Luck);
        writer.Write(value.ResistEarth); writer.Write(value.ResistFire); writer.Write(value.ResistAir); writer.Write(value.ResistWater); writer.Write(value.ResistNeutral);
    }

    private static PlayerStatsSnapshot ReadPlayerStats(PacketReader reader)
    {
        var value = new PlayerStatsSnapshot(
            reader.ReadInt32(), reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt32(),
            ReadAttribute(reader), ReadAttribute(reader), ReadAttribute(reader), ReadAttribute(reader), ReadAttribute(reader),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadInt32(),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        if (value.Level < 1 || value.Experience < 0 || value.ExperienceToNextLevel < 0 || value.AvailableAttributePoints < 0 ||
            value.MaxHealth < 1 || value.Health < 0 || value.Health > value.MaxHealth || value.MaxMana < 0 || value.Mana < 0 || value.Mana > value.MaxMana ||
            !float.IsFinite(value.Defense) || !float.IsFinite(value.ManaRegenPerSecond) || !float.IsFinite(value.OutOfCombatManaRegenPerSecond))
            throw new InvalidDataException("Stats de jugador inválidos.");
        return value;
    }

    private static void WriteCombatDebug(PacketWriter writer, CombatDebugPacket value)
    {
        if (value.Target.Value <= 0 || !Enum.IsDefined(value.Attack) || !Enum.IsDefined(value.DamageType) || !Enum.IsDefined(value.ScalingAttribute))
            throw new InvalidDataException("Telemetría de combate inválida.");
        writer.Write(value.Target.Value);
        writer.Write((byte)value.Attack);
        writer.Write((byte)value.DamageType);
        writer.Write((byte)value.ScalingAttribute);
        writer.Write(value.RawDamage);
        writer.Write(value.ResistancePercent);
        writer.Write(value.AppliedDamage);
        writer.Write(value.Critical);
        writer.Write(value.TargetHealth);
        writer.Write(value.TargetMaxHealth);
        writer.Write(value.Dps5Seconds);
        writer.Write(value.Dps10Seconds);
        writer.Write(value.TotalDamage);
        writer.Write(value.Hits);
        writer.Write(value.CriticalHits);
    }

    private static CombatDebugPacket ReadCombatDebug(PacketReader reader)
    {
        var value = new CombatDebugPacket(
            new EntityId(reader.ReadInt64()),
            (DevelopmentAttackKind)reader.ReadByte(),
            (Element)reader.ReadByte(),
            (PrimaryAttributeId)reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadInt32(),
            reader.ReadBool(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadInt64(),
            reader.ReadInt32(),
            reader.ReadInt32());
        if (value.Target.Value <= 0 || !Enum.IsDefined(value.Attack) || !Enum.IsDefined(value.DamageType) || !Enum.IsDefined(value.ScalingAttribute) ||
            !float.IsFinite(value.RawDamage) || value.RawDamage < 0 || !float.IsFinite(value.ResistancePercent) ||
            value.AppliedDamage < 0 || value.TargetMaxHealth < 1 || value.TargetHealth < 0 || value.TargetHealth > value.TargetMaxHealth ||
            !float.IsFinite(value.Dps5Seconds) || value.Dps5Seconds < 0 || !float.IsFinite(value.Dps10Seconds) || value.Dps10Seconds < 0 ||
            value.TotalDamage < 0 || value.Hits < 0 || value.CriticalHits < 0 || value.CriticalHits > value.Hits)
            throw new InvalidDataException("Telemetría de combate inválida.");
        return value;
    }

    private static void WriteEntity(PacketWriter writer, EntityState entity)
    {
        writer.Write(entity.Id.Value); writer.Write((byte)entity.Kind); writer.Write(entity.Definition is not null);
        if (entity.Definition is { } definition) writer.Write(definition.Value);
        writer.Write(entity.MapInstance.Value); writer.Write(entity.Position); writer.Write(entity.Velocity);
        writer.Write((byte)entity.Facing); writer.Write(entity.VisualKey); writer.Write(entity.DisplayName, 128);
        writer.Write(entity is PlayerState);
        if (entity is PlayerState player) writer.Write(player.Character.Value);
        writer.Write(entity is WorldItemState);
        if (entity is WorldItemState item) writer.Write(item.ItemInstance.Value);
    }

    private static EntityState ReadEntity(PacketReader reader)
    {
        var id = new EntityId(reader.ReadInt64()); var kind = (EntityKind)reader.ReadByte();
        DefinitionId? definition = reader.ReadBool() ? new(reader.ReadGuid()) : null;
        var instance = new MapInstanceId(reader.ReadInt64()); var position = reader.ReadVector2(); var velocity = reader.ReadVector2();
        var facing = (Direction)reader.ReadByte();
        var visual = reader.ReadContentKey(); var name = reader.ReadString(128);
        CharacterId? character = reader.ReadBool() ? new(reader.ReadGuid()) : null;
        ItemInstanceId? item = reader.ReadBool() ? new(reader.ReadGuid()) : null;
        if (id.Value <= 0 || instance.Value <= 0 || !position.IsFinite || !velocity.IsFinite) throw new InvalidDataException("Entidad inválida.");
        return kind switch
        {
            EntityKind.Player when character is { } value => new PlayerState(id, value, instance, position, velocity, facing, visual, name),
            EntityKind.Mob when definition is { } value => new MobState(id, value, instance, position, velocity, facing, visual, name),
            EntityKind.Npc when definition is { } value => new NpcState(id, value, instance, position, velocity, facing, visual, name),
            EntityKind.Resource when definition is { } value => new ResourceState(id, value, instance, position, facing, visual, name),
            EntityKind.Projectile => new ProjectileState(id, definition, instance, position, velocity, facing, visual, name),
            EntityKind.WorldItem when definition is { } value && item is { } itemValue => new WorldItemState(id, value, itemValue, instance, position, facing, visual, name),
            EntityKind.InteractiveObject => new EntityState(id, kind, definition, instance, position, velocity, facing, visual, name),
            _ => throw new InvalidDataException("Entidad incompleta para su tipo.")
        };
    }

    private static void WriteCount(PacketWriter writer, int count, int maximum)
    {
        if (count < 0 || count > maximum) throw new InvalidDataException("Colección demasiado grande.");
        writer.Write((ushort)count);
    }
    private static T[] ReadArray<T>(PacketReader reader, int maximum, Func<T> read)
    {
        var count = reader.ReadUInt16();
        if (count > maximum) throw new InvalidDataException("Colección demasiado grande.");
        var result = new T[count]; for (var index = 0; index < count; index++) result[index] = read();
        return result;
    }
}
