using LoggingWayPlugin.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Providers
{
    public delegate void NotifyNewCombatEvent(CombatEvent combatEvent);
    public interface IProvider
    {
        public event NotifyNewCombatEvent? OnNewCombatEvent;
    }
}
