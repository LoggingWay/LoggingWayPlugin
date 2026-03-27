using LoggingWayPlugin.Events;
using LoggingWayPlugin.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Parser
{
    public class DebugParser : IDisposable
    {
        public IProvider _provider;
        private Configuration _configuration;
        public DebugParser(IProvider provider,Configuration configuration)
        {
            _provider = provider;
            _provider.OnNewCombatEvent += HandleNewCombatEvent;
            _configuration = configuration;
        }

        public void Dispose()
        {
            _provider.OnNewCombatEvent -= HandleNewCombatEvent;
        }

        private void HandleNewCombatEvent(Proto.CombatEvent combatEvent)
        {
            if (_configuration.OutputEventsToLog)
            {
                Service.Log.Debug(combatEvent.ToString());
            }
        }
    }
}
