using Google.Protobuf.WellKnownTypes;
using SamplePlugin.Events;
using SamplePlugin.Proto;
using System;
using System.Collections.Generic;
using System.Text;
using static FFXIVClientStructs.FFXIV.Client.System.String.Utf8String.Delegates;
using static SamplePlugin.Events.CombatEvent;

namespace SamplePlugin
{
    public static class ProtoExtensions
    {

        public static Proto.CombatEvent ToProto(this Events.CombatEvent evt)
        {
            var protoEvent = new Proto.CombatEvent
            {
                TimestampEpochMs = evt.Timestamp.ToUnixTimeMilliseconds(),
            };

            // Convert entities
            if (evt.Source != null)
            {
                protoEvent.Source = evt.Source.ToProto();
            }

            if (evt.Target != null)
            {
                protoEvent.Target = evt.Target.ToProto();
            }

            // Convert snapshot
            if (evt.SourceSnapshot != null)
            {
                protoEvent.SourceSnapshot = evt.SourceSnapshot.ToProto();
            }

            if (evt.TargetSnapshot != null)
            {
                protoEvent.TargetSnapshot = evt.TargetSnapshot.ToProto();
            }
            // Convert discriminated union data
            switch (evt.Data)
            {
                case CombatEventData.StatusEffect status:
                    protoEvent.StatusEffect = status.ToProto();
                    break;

                case CombatEventData.HoT hot:
                    protoEvent.Hot = hot.ToProto();
                    break;

                case CombatEventData.DoT dot:
                    protoEvent.Dot = dot.ToProto();
                    break;

                case CombatEventData.DamageTaken damage:
                    protoEvent.DamageTaken = damage.ToProto();
                    break;

                case CombatEventData.Healed healed:
                    protoEvent.Healed = healed.ToProto();
                    break;

                case CombatEventData.Death:
                    protoEvent.Death = new DeathData();
                    break;

                case CombatEventData.EncounterStart encounterStart:
                    protoEvent.EncounterStart = encounterStart.ToProto();
                    break;

                case CombatEventData.EncounterEnd encounterEnd:
                    protoEvent.EncounterEnd = encounterEnd.ToProto();
                    break;
                case CombatEventData.ZoneChange zoneChange:
                    protoEvent.ZoneChange = zoneChange.ToProto();
                    break;
            }

            return protoEvent;
        }

        private static Proto.Entity ToProto(this Events.Entity entity)
        {
            return new Proto.Entity
            {
                Name = entity.Name ?? string.Empty,
                GameobjectId = entity.GameObjectId,
                Objectkind = (ObjectKind)entity.Kind
            };
        }
        private static Proto.EventSnapshot ToProto(this Events.EventSnapshot snapshot)
        {
            var protoSnapshot = new Proto.EventSnapshot
            {
                CurrentHp = snapshot.CurrentHp,
                MaxHp = snapshot.MaxHp,
                BarrierPercent = snapshot.BarrierPercent
            };

            if (snapshot.StatusEffects != null)
            {
                foreach (var effect in snapshot.StatusEffects)
                {
                    protoSnapshot.StatusEffects.Add(effect.ToProto());
                }
            }

            return protoSnapshot;
        }
        private static Proto.StatusEffectSnapshot ToProto(this Events.StatusEffectSnapshot snapshot)
        {
            return new Proto.StatusEffectSnapshot
            {
                Id = snapshot.Id,
                SourceId = snapshot.SourceId,
                StackCount = snapshot.StackCount
            };
        }
        private static StatusEffectData ToProto(this CombatEventData.StatusEffect data)
        {
            return new StatusEffectData
            {
                Id = data.Id,
                StackCount = data.StackCount,
                Icon = data.Icon ?? 0,
                Duration = data.Duration,
                Category = data.Category.ToProto()
            };
        }

        private static HoTData ToProto(this CombatEventData.HoT data)
        {
            return new HoTData { Amount = data.Amount };
        }

        private static DoTData ToProto(this CombatEventData.DoT data)
        {
            return new DoTData { Amount = data.Amount };
        }

        private static DamageTakenData ToProto(this CombatEventData.DamageTaken data)
        {
            return new DamageTakenData
            {
                Amount = data.Amount,
                ActionId = data.ActionId,
                Crit = data.Crit,
                DirectHit = data.DirectHit,
                DamageType = data.DamageType.ToProto(),
                DisplayType = data.DisplayType.ToProto(),
                Parried = data.Parried,
                Blocked = data.Blocked,
                Icon = data.Icon ?? 0
            };
        }

        private static HealedData ToProto(this CombatEventData.Healed data)
        {
            return new HealedData
            {
                Amount = data.Amount,
                ActionId = data.ActionId,
                Crit = data.Crit,
                Icon = data.Icon ?? 0
            };
        }

        private static EncounterStartData ToProto(this CombatEventData.EncounterStart data)
        {
            return new EncounterStartData
            {
                Territorytype = data.TerritoryType,
            };
        }
        private static EncounterEndData ToProto(this CombatEventData.EncounterEnd data)
        {
            return new EncounterEndData
            {
                Territorytype = data.TerritoryType,
            };
        }

        private static ZoneChangeData ToProto(this CombatEventData.ZoneChange data)
        {
            return new ZoneChangeData
            {
                Territorytype = data.TerritoryType
            };
        }
        private static Proto.StatusCategory ToProto(this Events.StatusCategory category)
        {
            return category switch
            {
                Events.StatusCategory.None => Proto.StatusCategory.None,
                Events.StatusCategory.Detrimental => Proto.StatusCategory.Detrimental,
                Events.StatusCategory.Beneficial => Proto.StatusCategory.Beneficial,
                _ => Proto.StatusCategory.None
            };
        }

        private static Proto.DamageType ToProto(this Events.DamageType damageType)
        {
            return damageType switch
            {
                Events.DamageType.Physical => Proto.DamageType.Physical,
                Events.DamageType.Magic => Proto.DamageType.Magic,
                Events.DamageType.Unknown => Proto.DamageType.Unknown,
                Events.DamageType.LimitBreak => Proto.DamageType.LimitBreak,
                Events.DamageType.Slashing => Proto.DamageType.Slashing,
                Events.DamageType.Piercing => Proto.DamageType.Piercing,
                Events.DamageType.Blunt => Proto.DamageType.Blunt,
                Events.DamageType.Shot => Proto.DamageType.Shot,
                Events.DamageType.Breath => Proto.DamageType.Breath,
                _ => Proto.DamageType.Unknown
            };
        }

        private static Proto.ActionType ToProto(this FFXIVClientStructs.FFXIV.Client.Game.ActionType actionType)
        {
            return (Proto.ActionType)actionType;
        }

        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
            {
                return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
    }
}
