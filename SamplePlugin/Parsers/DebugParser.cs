using SamplePlugin.Events;
using SamplePlugin.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SamplePlugin.Parser
{
    public class DebugParser : IDisposable
    {
        public IProvider _provider;
        public DebugParser(IProvider provider)
        {
            _provider = provider;
            _provider.OnNewCombatEvent += HandleNewCombatEvent;
        }

        public void Dispose()
        {
            _provider.OnNewCombatEvent -= HandleNewCombatEvent;
        }

        private void HandleNewCombatEvent(CombatEvent combatEvent)
        {
            Service.Log.Debug(combatEvent.ToString());
        }
    }
}
