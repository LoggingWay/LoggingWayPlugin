using SamplePlugin.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace SamplePlugin.Parsers
{
    public interface IParser
    {
        public void StartEncounter();
        public void EndEncounter();
    }
}
