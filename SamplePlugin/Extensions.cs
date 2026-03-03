using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using SamplePlugin.Events;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
namespace SamplePlugin;

public static class Extensions {
    public static unsafe byte Barrier(this IPlayerCharacter player) {
        return ((Character*)player.Address)->CharacterData.ShieldValue;
    }

   
    public static unsafe EventSnapshot CreateSnapshot(BattleChara* battleChara) {
        List<StatusEffectSnapshot> statusEffects = new List<StatusEffectSnapshot>();
        for (int i = 0; i < battleChara->StatusManager.NumValidStatuses; i++)
        {
            Status? s = battleChara->StatusManager.Status[i];
            if (s != null && s.Value.StatusId != 0)
            {
                statusEffects.Add(new StatusEffectSnapshot(s.Value.StatusId, s.Value.SourceObject.ObjectId, s.Value.Param));
            }
        }
        return new EventSnapshot {
            CurrentHp = battleChara->Health,
            MaxHp = battleChara->MaxHealth,
            StatusEffects = statusEffects,
            BarrierPercent = 0
        };
    }
    public static EventSnapshot Snapshot(
        this IPlayerCharacter player, bool snapEffects = false,
        IReadOnlyCollection<uint>? additionalStatus = null) {
        var statusEffects = snapEffects
            ? player.StatusList.Select(s => new StatusEffectSnapshot(s.StatusId, s.SourceId, s.Param))
                .ToList()
            : null;
        if (additionalStatus != null)
            statusEffects?.AddRange(additionalStatus.Select(s => new StatusEffectSnapshot(s, 0, 0)));
        var snapshot = new EventSnapshot {
            CurrentHp = player.CurrentHp,
            MaxHp = player.MaxHp,
            StatusEffects = statusEffects,
            BarrierPercent = player.Barrier()
        };
        return snapshot;
    }

    public static EventSnapshot Snapshot(
        this IBattleChara battleChara,bool snapEffects = false,IReadOnlyCollection<uint>?
        additionalStatus = null)
    {
        var statusEffects = snapEffects
            ? battleChara.StatusList.Select(s =>  new StatusEffectSnapshot(s.StatusId, s.SourceId,s.Param))
            .ToList()
            : null;
        if (additionalStatus != null)
            statusEffects?.AddRange(additionalStatus.Select(s => new StatusEffectSnapshot(s,0,0)));
        var snapshot = new EventSnapshot
        {
            CurrentHp = battleChara.CurrentHp,
            MaxHp = battleChara.MaxHp,
            StatusEffects = statusEffects,
            BarrierPercent = 0
        };
        return snapshot;
    }
}
