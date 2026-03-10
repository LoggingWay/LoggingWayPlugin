using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Game
{
    //various constants that can't be resolved from sheets
    internal static class GameConstants
    {
        public static List<byte> Casters = [6, 7, 24, 25, 26, 27, 28, 33, 35, 36, 40, 42];
        public static List<byte> Physical_Ranged = [5, 23, 31, 38];

        public static List<byte> Tanks = [1, 3, 19, 21, 32, 37];
    }
}
