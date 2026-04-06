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
    public enum JobId : byte
    {
        ADV = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BTN = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
        RPR = 39,
        SGE = 40,
        VPR = 41,
        PCT = 42,
    }
}
