using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static LoggingWayPlugin.Parsers.DamageParser;

namespace LoggingWayPlugin
{
    public static class Utils
    {
        public static uint GetJobIdForPlayer(ulong? objectId)
        {
            if (objectId == null)
                return 0;
            if (Service.ObjectTable.SearchById(objectId.Value) is not IPlayerCharacter p)
                return 0;
          return p.ClassJob.RowId;
        }

        public static string GetNameOfGameObject(ulong? objectId)
        {
            if(objectId == null)
                return "unknown";
            if (Service.ObjectTable.SearchById(objectId.Value) is not ICharacter p)
                return "unknown";
            return p.Name.ToString() ?? "unknown";
        }
        public static string GetCurrentZoneName()
        {
            return Service.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Service.ClientState.TerritoryType)!.PlaceName.Value.Name.ToString() ?? "Unknown";
        }

        
    }
}
