using LoggingWayPlugin.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Parsers
{
    public interface IParser
    {
        public void StartEncounter();
        public void EndEncounter();
    }
}
