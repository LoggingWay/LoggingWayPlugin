using FFXIVClientStructs.FFXIV.Client.Game.UI;
using SamplePlugin.Events;
using SamplePlugin.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using static SamplePlugin.Events.CombatEvent;

namespace SamplePlugin.Parsers
{
    public class DamageParser : IDisposable,IParser
    {
        public record CombattantInfo
        {
            public string Name { get; init; } = "Unknown";
            public uint JobId { get; init; }

            public uint TotalDamage { get; set; } = 0;
            public uint TotalHealing { get; set; } = 0;
            public int HitCount { get; set; } = 0;
            public int CritCount { get; set; } = 0;
            public int DirectHitCount { get; set; } = 0;
            public int CritDirectHitCount { get; set; } = 0;
            public uint MaxHit { get; set; } = 0;

            public int Deaths { get; set; } = 0;
            public Dictionary<string, ActionInfo> ActionsBreakdown { get; init; } = new();

            public override string ToString()
            {
                return $"JobId: {JobId}, TotalDamage: {TotalDamage}, HitCount: {HitCount}, CritCount: {CritCount}, DirectHitCount: {DirectHitCount}, MaxHit: {MaxHit}";
            }

            public string GetActionBreakdownString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var action in ActionsBreakdown)
                {
                    sb.AppendLine($"Action: {action.Key}, Info: {action.Value.ToString()}");
                }
                return sb.ToString();
            }

        }

        public record ActionInfo
        {
            public int TotalUses { get; set; } = 0;
            public uint TotalDamage { get; set; } = 0;
            public int HitCount { get; set; } = 0;
            public int CritCount { get; set; } = 0;
            public int DirectHitCount { get; set; } = 0;
            public uint MaxHit { get; set; } = 0;

            public override string ToString()
            {
                return $"TotalUses: {TotalUses}, TotalDamage: {TotalDamage}, HitCount: {HitCount}, CritCount: {CritCount}, DirectHitCount: {DirectHitCount}, MaxHit: {MaxHit}";
            }
        }


        public record EncounterInfo
        {
            public DateTime start { get; init; }
            public DateTime end { get; init; }
            public TimeSpan Duration { get; init; }
            public required ConcurrentDictionary<string, CombattantInfo> DamageCounts { get; init; } = new();
        }
        public IProvider _provider;

        public ConcurrentDictionary<string,CombattantInfo> damageCounts { get; private set; } = new();
        public ConcurrentDictionary<string,EncounterInfo> encounterHistory = new();
        public DateTime encounterStartTime { get; private set; }
        public DateTime encounterEndTime { get; private set; }
        public System.Timers.Timer encounterResetTimer { get; private set; }
        public bool encounterActive { get; private set; } = false;
        public string encounterId { get; private set; } = "";

        public int encounterTimeoutMs { get; private set; } = 2000;
        public DamageParser(IProvider provider,Configuration config)
        {
            _provider = provider;
            _provider.OnNewCombatEvent += HandleNewCombatEvent;
            encounterResetTimer = new System.Timers.Timer(20000);
            encounterResetTimer.Elapsed += EndEncounterTimer;
            encounterTimeoutMs = config.EncounterEndDelayMs;
        }

        public void Dispose()
        {
            _provider.OnNewCombatEvent -= HandleNewCombatEvent;
        }

        public void StartEncounter()
        {
            damageCounts.Clear();
            encounterStartTime = DateTime.Now;
            encounterId = Utils.GetCurrentZoneName() + " " + encounterStartTime.ToString("HHmmss");
            encounterActive = true;
            encounterResetTimer.Interval = encounterTimeoutMs;
            encounterResetTimer.Start();
            Service.Log.Verbose($"Encounter {encounterId} started.");
        }

        public void EndEncounter()
        {
            var encounterDuration = DateTime.Now - encounterStartTime;
            var encounterInfo = new EncounterInfo
            {
                start = encounterStartTime,
                end = DateTime.Now,
                Duration = encounterDuration,
                DamageCounts = new ConcurrentDictionary<string, CombattantInfo>(damageCounts)
            };
            encounterHistory[encounterId] = encounterInfo;
            encounterActive = false;
            encounterEndTime = DateTime.Now;
            encounterResetTimer.Stop();
            Service.Log.Verbose($"Encounter {encounterId} ended. Duration: {encounterDuration.TotalSeconds} seconds.");
        }
        public void EndEncounterTimer(Object source, ElapsedEventArgs e)
        {
            EndEncounter();
        }
        private void HandleNewCombatEvent(CombatEvent combatEvent)
        {
            switch (combatEvent.Data)
            {
                case CombatEventData.EncounterStart:
                    StartEncounter();
                    break;
                case CombatEventData.EncounterEnd:
                    EndEncounter();
                    break;
                case CombatEventData.StatusEffect statusEffect:
                    // Handle status effect event
                    break;
                case CombatEventData.HoT hot:
                    // Handle heal over time event
                    break;
                case CombatEventData.DoT dot:
                    var DoTInfo = damageCounts.GetOrAdd("DoT", _ => new CombattantInfo { Name = "DoT", JobId = 0 });
                    DoTInfo.TotalDamage = DoTInfo.TotalDamage + dot.Amount;
                    damageCounts.AddOrUpdate("DoT", DoTInfo, (_, _) => DoTInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                case CombatEventData.DamageTaken damageTaken:
                    // General breakdown
                    if (combatEvent.Source?.Kind != FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Pc)
                        return;
                    var combatantInfo = damageCounts.GetOrAdd(combatEvent.Source.Name ?? "Unknown", _ => new CombattantInfo {Name = combatEvent.Source.Name, JobId = 0 });
                    combatantInfo.TotalDamage = combatantInfo.TotalDamage + damageTaken.Amount;
                    combatantInfo.HitCount = combatantInfo.HitCount + 1;
                    if (damageTaken.Crit && damageTaken.DirectHit)
                    {
                        combatantInfo.CritDirectHitCount = combatantInfo.CritDirectHitCount + 1;
                    }
                    else
                    {
                        combatantInfo.CritCount = damageTaken.Crit ? combatantInfo.CritCount + 1 : combatantInfo.CritCount;
                        combatantInfo.DirectHitCount = damageTaken.DirectHit ? combatantInfo.DirectHitCount + 1 : combatantInfo.DirectHitCount;
                    }
                    combatantInfo.MaxHit = Math.Max(combatantInfo.MaxHit,damageTaken.Amount);
                    //per-action breakdown
                    var actionInfo = combatantInfo.ActionsBreakdown.GetValueOrDefault(damageTaken.Action) ?? new ActionInfo();
                    actionInfo.TotalUses = actionInfo.TotalUses + 1;
                    actionInfo.TotalDamage = actionInfo.TotalDamage + damageTaken.Amount;
                    actionInfo.HitCount = actionInfo.HitCount + 1;
                    actionInfo.CritCount = damageTaken.Crit ? actionInfo.CritCount + 1 : actionInfo.CritCount;
                    actionInfo.DirectHitCount = damageTaken.DirectHit ? actionInfo.DirectHitCount + 1 : actionInfo.DirectHitCount;
                    combatantInfo.ActionsBreakdown[damageTaken.Action] = actionInfo;

                    damageCounts.AddOrUpdate(combatEvent.Source.Name ?? "Unknown", combatantInfo, (_, _) => combatantInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                case CombatEventData.Healed healed:
                    var combatantHealInfo = damageCounts.GetOrAdd(combatEvent.Source.Name ?? "Unknown", _ => new CombattantInfo { Name = combatEvent.Source.Name, JobId = 0 });
                    combatantHealInfo.TotalHealing = combatantHealInfo.TotalHealing + healed.Amount;
                    damageCounts.AddOrUpdate(combatEvent.Source.Name ?? "Unknown", combatantHealInfo,(_, _) => combatantHealInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                case CombatEventData.Death death:
                    var combatantDeathInfo = damageCounts.GetOrAdd(combatEvent.Source.Name ?? "Unknown", _ => new CombattantInfo { Name = combatEvent.Source.Name, JobId = 0 });
                    combatantDeathInfo.Deaths = combatantDeathInfo.Deaths + 1;
                    damageCounts.AddOrUpdate(combatEvent.Source.Name ?? "Unknown", combatantDeathInfo, (_,_) => combatantDeathInfo);
                    encounterResetTimer.Interval = encounterTimeoutMs;
                    break;
                default:
                    // Handle unknown combat event type
                    break;
            }
        }
    }
}
