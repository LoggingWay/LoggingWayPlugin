using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using LoggingWayPlugin;
using LoggingWayPlugin.Events;
using LoggingWayPlugin.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.System.Scheduler.Resource.SchedulerResource;
using Action = Lumina.Excel.Sheets.Action;
using Status = Lumina.Excel.Sheets.Status;

namespace LoggingWayPlugin.Providers;

public class PacketHandlersHooks : IDisposable,IProvider
{

    private unsafe delegate void ProcessPacketActionEffectDelegate(
        uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds);

    private delegate void ProcessPacketActorControlDelegate(
        uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
        byte param9);

    private delegate void ProcessPacketEffectResultDelegate(uint targetId, IntPtr actionIntegrityData, byte isReplay);

    private readonly Hook<ProcessPacketActionEffectDelegate> processPacketActionEffectHook;

    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private readonly Hook<ProcessPacketActorControlDelegate> processPacketActorControlHook = null!;

    [Signature("48 8B C4 44 88 40 18 89 48 08", DetourName = nameof(ProcessPacketEffectResultDetour))]
    private readonly Hook<ProcessPacketEffectResultDelegate> processPacketEffectResultHook = null!;

    public event NotifyNewCombatEvent? OnNewCombatEvent;
    private List<ulong> currentCombatantIds = [];
    private bool inEncounter = false;
    public unsafe PacketHandlersHooks()
    {
        Service.Log.Debug("Initializing PacketHandlersHooks");
        Service.GameInteropProvider.InitializeFromAttributes(this);

        processPacketActionEffectHook =
            Service.GameInteropProvider.HookFromSignature<ProcessPacketActionEffectDelegate>(ActionEffectHandler.Addresses.Receive.String,
                ProcessPacketActionEffectDetour);
        processPacketActionEffectHook.Enable();
        processPacketActorControlHook.Enable();
        processPacketEffectResultHook.Enable();
        Service.Log.Debug("Hooks enabled");
        Service.DutyState.DutyStarted += OnEncounterStart;
        Service.DutyState.DutyRecommenced += OnEncounterStart;
        Service.DutyState.DutyWiped += OnEncounterEndWipe;
        Service.DutyState.DutyCompleted += OnEncounterEndComplete;
        Service.ClientState.TerritoryChanged += OnTerritoryChange;
        
    }

    private void OnTerritoryChange(ushort e)
    {
        OnNewCombatEvent?.Invoke(new Proto.CombatEvent { TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(), ZoneChange = new Proto.ZoneChangeData { Territorytype = e} });
    }

    private void OnEncounterStart(object? sender, ushort e)
    {
        Service.Log.Verbose($"Encounter start:{e}");
        inEncounter = true;
        OnNewCombatEvent?.Invoke(new Proto.CombatEvent { TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(), EncounterStart = new Proto.EncounterStartData { Territorytype = e } });
    }

    private void OnEncounterEndWipe(object? sender, ushort e)
    {
        Service.Log.Verbose($"Encounter end:{e}");
        inEncounter = false;
        currentCombatantIds.Clear();
        OnNewCombatEvent?.Invoke(new Proto.CombatEvent { TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(), EncounterEnd = new Proto.EncounterEndData { Territorytype = e, Reason = Proto.EncounterEndKind.Wipe } });
    }

    private void OnEncounterEndComplete(object? sender, ushort e)
    {
        Service.Log.Verbose($"Encounter end:{e}");
        inEncounter = false;
        currentCombatantIds.Clear();
        OnNewCombatEvent?.Invoke(new Proto.CombatEvent { TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(), EncounterEnd = new Proto.EncounterEndData { Territorytype = e,Reason = Proto.EncounterEndKind.Clear} });
    }

    private unsafe void ProcessPacketActionEffectDetour(
        uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* effectHeader, ActionEffectHandler.TargetEffects* effectArray,
        GameObjectId* targetEntityIds)
    {
        processPacketActionEffectHook.Original(casterEntityId, casterPtr, targetPos, effectHeader, effectArray, targetEntityIds);
        try
        {
            if (effectHeader->NumTargets == 0)
                return;
            var actionId = (ActionType)effectHeader->ActionType switch
            {
                ActionType.Mount => 0xD000000 + effectHeader->ActionId,
                ActionType.Item => 0x2000000 + effectHeader->ActionId,
                _ => effectHeader->SpellId
            };

            for (var i = 0; i < effectHeader->NumTargets; i++)
            {
                var actionTargetId = (uint)(targetEntityIds[i] & uint.MaxValue);
                if (Service.ObjectTable.SearchById(actionTargetId) is not IBattleChara p)
                    continue;
                for (var j = 0; j < 8; j++)
                {
                    ref var actionEffect = ref effectArray[i].Effects[j];
                    if (actionEffect.Type == 0)
                        continue;
                    uint amount = actionEffect.Value;
                    if ((actionEffect.Param4 & 0x40) == 0x40)
                        amount += (uint)actionEffect.Param3 << 16;
                    EffectToCombatEvent(casterEntityId, casterPtr, effectHeader, actionId, p, actionEffect, amount);
                }
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }


    }

    private unsafe void EffectToCombatEvent(uint casterEntityId, Character* casterPtr, ActionEffectHandler.Header* effectHeader, uint actionId,IBattleChara p, ActionEffectHandler.Effect actionEffect, uint amount)
    {
        Action? action = null;
        string? source = null;
        ulong? sourceGameObjectId = null;
        uint? sourceEntityId = null;
        uint? sourceBaseId = null;
        ObjectKind? sourceObjectKind = null;
        List<uint>? additionalStatus = null;
        
        string? target = null;
        uint? targetEntityId = null;
        ulong? targetGameObjectId = null;
        uint? targetBaseId = null;
        ObjectKind? targetObjectKind = null;
        action ??= Service.DataManager.GetExcelSheet<Action>().GetRowOrDefault(actionId);
        source ??= casterPtr->NameString;
        sourceEntityId ??= casterEntityId;
        sourceGameObjectId ??= casterPtr->GetGameObjectId().Id;
        sourceBaseId ??= casterPtr->BaseId;
        sourceObjectKind ??= casterPtr->ObjectKind;
        
        target ??= p.Name.TextValue;
        targetEntityId ??= p.EntityId;
        targetGameObjectId ??= ((BattleChara*)p.Address)->GetGameObjectId().Id;
        targetBaseId ??= ((BattleChara*)p.Address)->BaseId;
        targetObjectKind ??= (ObjectKind)p.ObjectKind;//
        switch ((ActionEffectType)actionEffect.Type)
        {
            case ActionEffectType.Miss:
            case ActionEffectType.Damage:
            case ActionEffectType.BlockedDamage:
            case ActionEffectType.ParriedDamage:
                if (additionalStatus == null)
                {
                    var statusManager = casterPtr->GetStatusManager();
                    additionalStatus = [];
                    if (statusManager != null)
                    {
                        foreach (ref var status in statusManager->Status)
                        {
                            if (status.StatusId is 1203 or 1195 or 1193 or 860 or 1715 or 2115 or 3642)
                                additionalStatus.Add(status.StatusId);
                        }
                    }
                }
                // 1203 = Addle2
                // 1195 = Feint
                // 1193 = Reprisal
                //  860 = Dismantled
                // 1715 = Malodorous, BLU Bad Breath
                // 2115 = Conked, BLU Magic Hammer
                // 3642 = Candy Cane, BLU Candy Cane
                newPlayerEvent((BattleChara*)casterPtr);
                OnNewCombatEvent?.Invoke(
                    new Proto.CombatEvent
                    {
                        TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                        SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)casterPtr),
                        TargetSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                        Source = new Proto.Entity
                        { 
                        GameobjectId = sourceGameObjectId ?? 0,
                          BaseId = sourceBaseId ?? 0,
                          Objectkind = (Proto.ObjectKind)(sourceObjectKind ?? ObjectKind.None)
                        },
                        Target = new Proto.Entity 
                        { 
                        GameobjectId = targetGameObjectId ?? 0,
                        BaseId = targetBaseId ?? 0,
                        Objectkind = (Proto.ObjectKind)(targetObjectKind ?? ObjectKind.None)
                        },
                        DamageTaken = new Proto.DamageTakenData
                        {
                            Amount = amount,
                            ActionId = actionId,
                            Icon = action?.Icon ?? 0,
                            Crit = (actionEffect.Param0 & 0x20) == 0x20,
                            DirectHit = (actionEffect.Param0 & 0x40) == 0x40,
                            DamageType = (Proto.DamageType)(DamageType)(actionEffect.Param1 & 0xF),
                            Parried = actionEffect.Type == (int)ActionEffectType.ParriedDamage,
                            Blocked = actionEffect.Type == (int)ActionEffectType.BlockedDamage,
                            DisplayType = (Proto.ActionType)(ActionType)effectHeader->ActionType
                        }
                    });
                break;
            case ActionEffectType.Heal:
                OnNewCombatEvent?.Invoke(
                    new Proto.CombatEvent
                    {
                        TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                        SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)casterPtr),
                        TargetSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                        Source = new Proto.Entity
                        {
                            GameobjectId = sourceGameObjectId ?? 0,
                            BaseId = sourceBaseId ?? 0,
                            Objectkind = (Proto.ObjectKind)(sourceObjectKind ?? ObjectKind.None)
                        },
                        Target = new Proto.Entity
                        {
                            GameobjectId = targetGameObjectId ?? 0,
                            BaseId = targetBaseId ?? 0,
                            Objectkind = (Proto.ObjectKind)(targetObjectKind ?? ObjectKind.None)
                        },
                        Healed = new Proto.HealedData
                        {
                            Amount = amount,
                            ActionId = actionId,
                            Icon = action?.Icon ?? 0,
                            Crit = (actionEffect.Param1 & 0x20) == 0x20
                        }
                    });
                break;
        }
    }

    private unsafe void ProcessPacketActorControlDetour(
        uint entityId, uint category, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId,
        byte param9)
    {
        processPacketActorControlHook.Original(entityId, category, param1, param2, param3, param4, param5, param6, param7, param8, targetId, param9);
        try
        {

            if (Service.ObjectTable.SearchById(entityId) is not IBattleChara p)
                return;
            ActorControlToCombatEvent(entityId, category, param1, param2, param4, p);//most param in Actor control are case specific 
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }
    }

    private unsafe void ActorControlToCombatEvent(uint entityId, uint category, uint param1, uint param2, uint param4, IBattleChara p)
    {
        var sourceName = p.Name.TextValue;
        var sourceEntityId = entityId;
        var sourceGameObjectId = ((BattleChara*)p.Address)->GetGameObjectId().Id;
        var sourceBaseId = ((BattleChara*)p.Address)->BaseId;
        var sourceObjectKind = ((BattleChara*)p.Address)->ObjectKind;
        switch ((ActorControlCategory)category)
        {
            case ActorControlCategory.DoT:
                OnNewCombatEvent?.Invoke(new Proto.CombatEvent
                {
                    TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                    SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                    Source = new Proto.Entity
                    {
                        GameobjectId = sourceGameObjectId,
                        BaseId = sourceBaseId,
                        Objectkind = (Proto.ObjectKind)sourceObjectKind
                    },
                    Dot = new Proto.DoTData
                    {
                        Amount = param2
                    }
                });
                break;
            case ActorControlCategory.HoT:
                if (param1 != 0)
                {
                    var status = Service.DataManager.GetExcelSheet<Status>().GetRowOrDefault(param1);
                    OnNewCombatEvent?.Invoke(
                        new Proto.CombatEvent
                        {
                            TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                            SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                            Source = new Proto.Entity
                            {
                                GameobjectId = sourceGameObjectId,
                                BaseId = sourceBaseId,
                                Objectkind = (Proto.ObjectKind)sourceObjectKind
                            },
                            Healed = new Proto.HealedData
                            {
                                Amount = param2,
                                ActionId = 0,
                                Icon = (uint)(ushort?)(status?.Icon),//lol TODO: do something better here
                                Crit = param4 == 1
                            }
                        });
                }
                else
                {
                    OnNewCombatEvent?.Invoke(new Proto.CombatEvent
                    {
                        TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                        SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                        Source = new Proto.Entity
                        {
                            GameobjectId = sourceGameObjectId,
                            BaseId = sourceBaseId,
                            Objectkind = (Proto.ObjectKind)sourceObjectKind
                        },
                        Hot = new Proto.HoTData
                        {
                            Amount = param2
                        }
                    });
                }

                break;
            case ActorControlCategory.Death:
                {
                    OnNewCombatEvent?.Invoke(new Proto.CombatEvent
                    {
                        TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                        SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                        Source = new Proto.Entity
                        {
                            GameobjectId = sourceGameObjectId,
                            BaseId = sourceBaseId,
                            Objectkind = (Proto.ObjectKind)sourceObjectKind
                        },
                        Death = new Proto.DeathData { }
                    });
                    break;
                }
        }
    }

    private unsafe void ProcessPacketEffectResultDetour(uint targetId, IntPtr actionIntegrityData, byte isReplay)
    {
        processPacketEffectResultHook.Original(targetId, actionIntegrityData, isReplay);

        try
        {
            var message = (AddStatusEffect*)actionIntegrityData;

            if (Service.ObjectTable.SearchById(targetId) is not IBattleChara p)
                return;

            var effects = (StatusEffectAddEntry*)message->Effects;
            var effectCount = Math.Min(message->EffectCount, 4u);
            for (uint j = 0; j < effectCount; j++)
            {
                var effect = effects[j];
                var effectId = effect.EffectId;
                if (effectId <= 0)
                    continue;
                // negative durations will remove effect
                if (effect.Duration < 0)
                    continue;
                StatusEffectToCombatEvent(targetId, p, effect, effectId);
            }
        }
        catch (Exception e)
        {
            Service.Log.Error(e, "Caught unexpected exception");
        }
    }

    private unsafe void StatusEffectToCombatEvent(uint targetId, IBattleChara p, StatusEffectAddEntry effect, ushort effectId)
    {
        BattleChara* sourceActor = (BattleChara*)(Service.ObjectTable.SearchById(effect.SourceActorId)?.Address);
        if (sourceActor == null)
            return;
         ulong sourceGameObjectId = sourceActor->GetGameObjectId().Id;
         uint sourceEntityId = sourceActor->EntityId;
        uint sourceBaseId = sourceActor->BaseId;
        string source = sourceActor->NameString;
        ObjectKind sourceObjectKind = sourceActor->ObjectKind;

        var status = Service.DataManager.GetExcelSheet<Status>().GetRowOrDefault(effectId);
        var targetIdStr = Service.ObjectTable.SearchById(targetId)?.Name.TextValue;
        OnNewCombatEvent?.Invoke(
            new Proto.CombatEvent
            {
                TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
                SourceSnapshot = Extensions.CreateSnapshot((BattleChara*)p.Address),
                Source = new Proto.Entity
                {
                    GameobjectId = sourceGameObjectId,
                    BaseId = sourceBaseId,
                    Objectkind = (Proto.ObjectKind)sourceObjectKind
                },
                StatusEffect = new Proto.StatusEffectData
                {

                    Id = effectId,
                    StackCount = effect.StackCount <= status?.MaxStacks ? effect.StackCount : 0u,
                    Icon = (uint)(ushort?)(status?.Icon),//TODO: same
                    Category = (Proto.StatusCategory)(status?.StatusCategory ?? 0),
                    Duration = effect.Duration
                }
            });
    }

    private unsafe void newPlayerEvent(BattleChara* combattant)
    {
        Service.Log.Debug("??");
        if (currentCombatantIds.Contains(combattant->GetGameObjectId()))
            return;
        currentCombatantIds.Add(combattant->GetGameObjectId());
        if (combattant->ObjectKind != ObjectKind.Pc)
            return;
        if (Service.ObjectTable.LocalPlayer?.GameObjectId != combattant->GetGameObjectId().Id)
            return;
        var State = UIState.Instance()->PlayerState;
        //for now we only log the local player
        OnNewCombatEvent?.Invoke(new Proto.CombatEvent
        {
            TimestampEpochMs = DateTime.UtcNow.ToUnixTimeMilliseconds(),
            SourceSnapshot = Extensions.CreateSnapshot(combattant),
            PlayerJoin = new Proto.PlayerEnterCombat
            {
               Name = combattant->NameString,
               ContentId = combattant->ContentId,
               HomeworldId = combattant->HomeWorld,
               GameobjectId = combattant->GetGameObjectId(),
               JobId = combattant->ClassJob,
                Level = combattant->Level,
                AttackPower = (uint)State.Attributes[GameConstants.Casters.Contains(State.CurrentClassJobId) ? 33 : 20],
                Skillspeed = (uint)State.Attributes[(int)PlayerAttribute.SkillSpeed],
                Spellspeed = (uint)State.Attributes[(int)PlayerAttribute.SpellSpeed],
                Tenacity = (uint)State.Attributes[(int)PlayerAttribute.Tenacity],
                Determination = (uint)State.Attributes[(int)PlayerAttribute.Determination],
                CriticalHit = (uint)State.Attributes[(int)PlayerAttribute.CriticalHit],
                DirectHit = (uint)State.Attributes[(int)PlayerAttribute.DirectHitRate],

            }
        });

    }
    public void Dispose()
    {

        Service.DutyState.DutyStarted -= OnEncounterStart;
        Service.DutyState.DutyRecommenced -= OnEncounterStart;
        Service.DutyState.DutyWiped -= OnEncounterEndWipe;
        Service.DutyState.DutyCompleted -= OnEncounterEndComplete;
        Service.ClientState.TerritoryChanged -= OnTerritoryChange;
        processPacketActionEffectHook.Dispose();
        processPacketEffectResultHook.Dispose();
        processPacketActorControlHook.Dispose();
    }
}
