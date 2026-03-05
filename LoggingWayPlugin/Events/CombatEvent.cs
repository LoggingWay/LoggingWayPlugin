using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using LoggingWayPlugin.Proto;
namespace LoggingWayPlugin.Events;


// Main discriminated union
public record CombatEvent
{
    // Common metadata for all events
    public required DateTime Timestamp { get; init; }
    public Entity? Source { get; init; }
    public Entity? Target { get; init; }
    public EventSnapshot? SourceSnapshot { get; init; }
    public EventSnapshot? TargetSnapshot { get; init; }

    // Discriminator
    public required CombatEventData Data { get; init; }

    public override string ToString()
    {
        var parts = new List<string>
        {
            $"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
            $"Source: {Source?.ToString() ?? "null"}",
            $"Target: {Target?.ToString() ?? "null"}",
            $"SourceSnapshot: {SourceSnapshot?.ToString() ?? "null"}",
            $"TargetSnapshot: {TargetSnapshot?.ToString() ?? "null"}",
            $"Data: {Data}"
        };
        return string.Join(" | ", parts);
    }
}

// Supporting types
public record Entity()
{
    public required ulong GameObjectId { get; init; }
    
    public string? Name { get; init; }
    public uint? BaseId { get; init; }
    public FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind Kind { get; init; }

    public override string ToString()
    {
        return $"GameObjectId: {GameObjectId.ToString() ?? "null"} | ObjectKind:{Kind.ToString()} | BaseId: {BaseId} | Name: {Name}";
    }
}

public record EventSnapshot
{
    public required uint CurrentHp { get; init; }
    public required uint MaxHp { get; init; }
    public IReadOnlyList<StatusEffectSnapshot>? StatusEffects { get; init; }
    public uint BarrierPercent { get; init; }

    public override string ToString()
    {
        var statusEffectsStr = StatusEffects != null && StatusEffects.Count > 0
            ? $"[{string.Join(", ", StatusEffects.Select(s => s.ToString()))}]"
            : "null";

        return $"(CurrentHp: {CurrentHp} | MaxHp: {MaxHp} | StatusEffects: {statusEffectsStr} | BarrierPercent: {BarrierPercent})";
    }
}

public record StatusEffectSnapshot(uint Id, uint SourceId, uint StackCount)
{
    public override string ToString()
    {
        return $"Id: {Id} | SourceId: {SourceId} | StackCount: {StackCount}";
    }
}


public abstract record CombatEventData
{
    public record StatusEffect : CombatEventData
    {
        public required uint Id { get; init; }
        public required uint StackCount { get; init; }
        public required ushort? Icon { get; init; }
        public required float Duration { get; init; }
        public string? Status { get; init; }
        public string? Description { get; init; }
        public required StatusCategory Category { get; init; }

        public override string ToString()
        {
            return $"StatusEffect[Id: {Id} | StackCount: {StackCount} | Icon: {Icon?.ToString() ?? "null"} | Duration: {Duration} | Status: {Status ?? "null"} | Description: {Description ?? "null"} | Category: {Category}]";
        }
    }

    public record HoT : CombatEventData
    {
        public required uint Amount { get; init; }

        public override string ToString()
        {
            return $"HoT[Amount: {Amount}]";
        }
    }

    public record DoT : CombatEventData
    {
        public required uint Amount { get; init; }

        public override string ToString()
        {
            return $"DoT[Amount: {Amount}]";
        }
    }

    public record DamageTaken : CombatEventData
    {
        public required uint Amount { get; init; }
        public required string Action { get; init; }
        public required uint ActionId { get; init; }
        public bool Crit { get; init; }
        public bool DirectHit { get; init; }
        public required DamageType DamageType { get; init; }
        public required FFXIVClientStructs.FFXIV.Client.Game.ActionType DisplayType { get; init; }
        public bool Parried { get; init; }
        public bool Blocked { get; init; }
        public ushort? Icon { get; init; }

        public override string ToString()
        {
            return $"DamageTaken[Amount: {Amount} | Action: {Action} | ActionId: {ActionId} | Crit: {Crit} | DirectHit: {DirectHit} | DamageType: {DamageType} | DisplayType: {DisplayType} | Parried: {Parried} | Blocked: {Blocked} | Icon: {Icon?.ToString() ?? "null"}]";
        }
    }

    public record Healed : CombatEventData
    {
        public required uint Amount { get; init; }
        public required string Action { get; init; }

        public required uint ActionId { get; init; }
        public bool Crit { get; init; }
        public ushort? Icon { get; init; }

        public override string ToString()
        {
            return $"Healed[Amount: {Amount} | Action: {Action}| ActionId: {ActionId} | Crit: {Crit} | Icon: {Icon?.ToString() ?? "null"}]";
        }
    }

    public record Death : CombatEventData
    {
        public override string ToString()
        {
            return "Death[]";
        }
    }

    public record EncounterStart : CombatEventData
    {
        public required ushort TerritoryType { get; init; }
        public override string ToString()
        {
            return $"EncounterStart[{TerritoryType}]";
        }
    }

    public record EncounterEnd : CombatEventData
    {
        public required ushort TerritoryType { get; init; }
        public override string ToString()
        {
            return $"EncounterEnd[{TerritoryType}]";
        }
    }

    public record ZoneChange : CombatEventData
    {
        public required ushort TerritoryType { get; init; }

        public override string ToString()
        {
            return $"ZoneChange[{TerritoryType}]";
        }
    }
}
