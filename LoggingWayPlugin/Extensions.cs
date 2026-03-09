using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using LoggingWayPlugin.Events;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
namespace LoggingWayPlugin;

public static class Extensions {
    public static unsafe byte Barrier(this IPlayerCharacter player) {
        return ((Character*)player.Address)->CharacterData.ShieldValue;
    }
    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    public static unsafe Proto.EventSnapshot CreateSnapshot(BattleChara* battleChara) {
        List<Proto.StatusEffectSnapshot> statusEffects = new List<Proto.StatusEffectSnapshot>();
        for (int i = 0; i < battleChara->StatusManager.NumValidStatuses; i++)
        {
            Status? s = battleChara->StatusManager.Status[i];
            if (s != null && s.Value.StatusId != 0)
            {
                statusEffects.Add(new Proto.StatusEffectSnapshot{
                    Id =  s.Value.StatusId, 
                    SourceId = s.Value.SourceObject.ObjectId, 
                    StackCount = s.Value.Param 
                });
            }
        }
        var protoSnapshot = new Proto.EventSnapshot
        {
            CurrentHp = battleChara->Health,
            MaxHp = battleChara->MaxHealth,
            BarrierPercent = 0
        };
            foreach (var effect in statusEffects)
            {
                protoSnapshot.StatusEffects.Add(effect);
            }
        return protoSnapshot;
    }
}
