using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using SamplePlugin.Proto;
using SamplePlugin.Providers;
using SamplePlugin.RPC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Timers;
using static SamplePlugin.Parsers.DamageParser;

namespace SamplePlugin.Parsers
{
    public class LoggingParser : IDisposable,IParser
    {
        public IProvider _provider;
        private Configuration _config;

        public DateTime encounterStartTime { get; private set; }
        public DateTime encounterEndTime { get; private set; }
        public System.Timers.Timer encounterResetTimer { get; private set; }
        public bool encounterActive { get; private set; } = false;
        public string encounterId { get; private set; } = "";

        public string logpath { get; private set; } = "";
        public int encounterTimeoutMs { get; private set; } = 2000;

        private FileStream? _logStream;//Some notes:CodedOutPutstream will close the underlying stream when disposed
        private CodedOutputStream? _codedOutputStream;
        private readonly CancellationTokenSource _cts = new();
        private LoggingwayManager _loggingwayManager;
        private ConcurrentQueue<Proto.CombatEvent> _eventQueue = new ConcurrentQueue<Proto.CombatEvent>();
        public LoggingParser(IProvider provider, Configuration config,LoggingwayManager loggingwayManager)
        {
            _provider = provider;
            _config = config;
            _provider.OnNewCombatEvent += HandleNewCombatEvent;
            encounterTimeoutMs = config.EncounterEndDelayMs;
            _loggingwayManager = loggingwayManager;
            logpath = config.Logpath;
        }


        private void HandleNewCombatEvent(Events.CombatEvent combatEvent)
        {
            if (!_config.EnableLoggingwayIntegration)
            {
                return; 
            }

            if (combatEvent.Data is Events.CombatEventData.EncounterStart)
            {
                StartEncounter();
            }
            if (combatEvent.Data is Events.CombatEventData.EncounterEnd)
            {
                EndEncounter();
            }
            if (encounterActive) //event can come through outside of encounters, which will throw an error either null writer or writing to a disposed stream
            {
                var proto = combatEvent.ToProto();
                _codedOutputStream?.WriteMessage(proto);
                _eventQueue.Enqueue(proto);
                //BatchAndSubmitEvents(proto);
            }

        }

        private void SubmitQueue()
        {
            try
            {
                Task.Run(async () =>
                 {

                     var eventsToSubmit = _eventQueue.AsEnumerable();
                     await _loggingwayManager.SubmitEncounter(eventsToSubmit);


                 });
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error submitting combat events: {ex.Message}");
            }
        }
       /* private void BatchAndSubmitEvents(Proto.CombatEvent combatEvent)
        {
            _eventQueue.Enqueue(combatEvent);
            if (_eventQueue.Count <= 10) // Arbitrary
                return;
            Task.Run(async () =>
            {
                var eventsToSubmit = new List<Proto.CombatEvent>();
                while (_eventQueue.TryDequeue(out var queuedCombatEvent))
                {
                    eventsToSubmit.Add(queuedCombatEvent);
                }
                if (eventsToSubmit.Count > 0)
                {
                    await _loggingwayManager.SubmitCombatEvents(eventsToSubmit.ToAsyncEnumerable());
                }
            });
            
        }
       */
        public void Dispose()
        {
            _provider.OnNewCombatEvent -= HandleNewCombatEvent;
            _codedOutputStream?.Dispose();
        }

        public void StartEncounter()
        {
            if (encounterActive)
            {
                Service.Log.Error("Start encounter event received but encounter is already running...");
            }
            encounterStartTime = DateTime.Now;
            encounterId = Utils.GetCurrentZoneName() + " " + encounterStartTime.ToString("HHmmss");
            encounterActive = true;
            //encounterResetTimer.Interval = encounterTimeoutMs;
            //encounterResetTimer.Start();
            _logStream = new FileStream(System.IO.Path.Combine(logpath, $"{encounterId}.proto"), FileMode.Create, FileAccess.Write, FileShare.Read);
            _eventQueue.Clear();
            _codedOutputStream = new CodedOutputStream(_logStream);
            Service.Log.Verbose($"Encounter {encounterId} started.");
        }

        public void EndEncounter()
        {
            if (!encounterActive) {
                Service.Log.Error("End encounter event received but no encounter is active");
                return; 
            }
            var encounterDuration = DateTime.Now - encounterStartTime;
            encounterActive = false;
            encounterEndTime = DateTime.Now;
            _codedOutputStream?.Flush();
            _codedOutputStream?.Dispose();
            SubmitQueue();
        }
    }
}
