using Dalamud.Game.ClientState.JobGauge.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Providers
{
    public static class ProtoMapper
    {
        public static Proto.AstrologianGauge MapAstrologian(ASTGauge g)
        {
            var msg = new Proto.AstrologianGauge
            {
                DrawnCrownCard = (Proto.CardType)g.DrawnCrownCard,
                ActiveDraw = (Proto.DrawType)g.ActiveDraw,
            };
            msg.DrawnCards.AddRange(g.DrawnCards.Select(c => (Proto.CardType)c));
            return msg;
        }

        public static Proto.BardGauge MapBard(BRDGauge g)
        {
            var msg = new Proto.BardGauge
            {
                SongTimer = g.SongTimer,
                Repertoire = g.Repertoire,
                SoulVoice = g.SoulVoice,
                Song = (Proto.Song)g.Song,
                LastSong = (Proto.Song)g.LastSong,
            };
            msg.Coda.AddRange(g.Coda.Select(c => (Proto.Song)c));
            return msg;
        }

        public static Proto.BlackMageGauge MapBlackMage(BLMGauge g) => new()
        {
            EnochianTimer = g.EnochianTimer,
            PolyglotStacks = g.PolyglotStacks,
            UmbralHearts = g.UmbralHearts,
            UmbralIceStacks = g.UmbralIceStacks,
            AstralFireStacks = g.AstralFireStacks,
            AstralSoulStacks = g.AstralSoulStacks,
            InUmbralIce = g.InUmbralIce,
            InAstralFire = g.InAstralFire,
            IsEnochianActive = g.IsEnochianActive,
            IsParadoxActive = g.IsParadoxActive,
        };

        public static Proto.DancerGauge MapDancer(DNCGauge g)
        {
            var msg = new Proto.DancerGauge
            {
                Feathers = g.Feathers,
                Esprit = g.Esprit,
                CompletedSteps = g.CompletedSteps,
                NextStep = g.NextStep,
                IsDancing = g.IsDancing,
            };
            msg.Steps.AddRange(g.Steps);
            return msg;
        }

        public static Proto.DarkKnightGauge MapDarkKnight(DRKGauge g) => new()
        {
            Blood = g.Blood,
            DarksideTimeRemaining = g.DarksideTimeRemaining,
            ShadowTimeRemaining = g.ShadowTimeRemaining,
            HasDarkArts = g.HasDarkArts,
            DeliriumComboStep = (Proto.DeliriumStep)g.DeliriumComboStep,
        };

        public static Proto.DragoonGauge MapDragoon(DRGGauge g) => new()
        {
            LotdTimer = g.LOTDTimer,
            IsLotdActive = g.IsLOTDActive,
            EyeCount = g.EyeCount,
            FirstmindsFocusCount = g.FirstmindsFocusCount,
        };

        public static Proto.GunbreakerGauge MapGunbreaker(GNBGauge g) => new()
        {
            Ammo = g.Ammo,
            MaxTimerDuration = g.MaxTimerDuration,
            AmmoComboStep = g.AmmoComboStep,
        };

        public static Proto.MachinistGauge MapMachinist(MCHGauge g) => new()
        {
            OverheatTimeRemaining = g.OverheatTimeRemaining,
            SummonTimeRemaining = g.SummonTimeRemaining,
            Heat = g.Heat,
            Battery = g.Battery,
            LastSummonBatteryPower = g.LastSummonBatteryPower,
            IsOverheated = g.IsOverheated,
            IsRobotActive = g.IsRobotActive,
        };

        public static Proto.MonkGauge MapMonk(MNKGauge g)
        {
            var msg = new Proto.MonkGauge
            {
                Chakra = g.Chakra,
                Nadi = (uint)g.Nadi,
                OpoOpoFury = (uint)g.OpoOpoFury,
                RaptorFury = (uint)g.RaptorFury,
                CoeurlFury = (uint)g.CoeurlFury,
                BlitzTimeRemaining = g.BlitzTimeRemaining,
            };
            msg.BeastChakra.AddRange(g.BeastChakra.Select(c => (Proto.BeastChakra)c));
            return msg;
        }

        public static Proto.NinjaGauge MapNinja(NINGauge g) => new()
        {
            Ninki = g.Ninki,
            Kazematoi = g.Kazematoi,
        };

        public static Proto.PaladinGauge MapPaladin(PLDGauge g) => new()
        {
            OathGauge = g.OathGauge,
        };

        public static Proto.PictomancerGauge MapPictomancer(PCTGauge g) => new()
        {
            PalleteGauge = g.PalleteGauge,
            Paint = g.Paint,
            CreatureMotifDrawn = g.CreatureMotifDrawn,
            WeaponMotifDrawn = g.WeaponMotifDrawn,
            LandscapeMotifDrawn = g.LandscapeMotifDrawn,
            MooglePortraitReady = g.MooglePortraitReady,
            MadeenPortraitReady = g.MadeenPortraitReady,
            CreatureFlags = (uint)g.CreatureFlags,
            CanvasFlags = (uint)g.CanvasFlags,
        };

        public static Proto.ReaperGauge MapReaper(RPRGauge g) => new()
        {
            Soul = g.Soul,
            Shroud = g.Shroud,
            EnshroudedTimeRemaining = g.EnshroudedTimeRemaining,
            LemureShroud = g.LemureShroud,
            VoidShroud = g.VoidShroud,
        };

        public static Proto.RedMageGauge MapRedMage(RDMGauge g) => new()
        {
            WhiteMana = g.WhiteMana,
            BlackMana = g.BlackMana,
            ManaStacks = g.ManaStacks,
        };

        public static Proto.SageGauge MapSage(SGEGauge g) => new()
        {
            AddersgallTimer = g.AddersgallTimer,
            Addersgall = g.Addersgall,
            Addersting = g.Addersting,
            Eukrasia = g.Eukrasia,
        };

        public static Proto.SamuraiGauge MapSamurai(SAMGauge g) => new()
        {
            Kaeshi = (Proto.Kaeshi)g.Kaeshi,
            Kenki = g.Kenki,
            MeditationStacks = g.MeditationStacks,
            Sen = (uint)g.Sen,
            HasSetsu = g.HasSetsu,
            HasGetsu = g.HasGetsu,
            HasKa = g.HasKa,
        };

        public static Proto.ScholarGauge MapScholar(SCHGauge g) => new()
        {
            Aetherflow = g.Aetherflow,
            FairyGauge = g.FairyGauge,
            SeraphTimer = g.SeraphTimer,
            DismissedFairy = (Proto.DismissedFairy)g.DismissedFairy,
        };

        public static Proto.SummonerGauge MapSummoner(SMNGauge g) => new()
        {
            SummonTimerRemaining = g.SummonTimerRemaining,
            AttunementTimerRemaining = g.AttunementTimerRemaining,
            ReturnSummon = (Proto.SummonPet)g.ReturnSummon,
            ReturnSummonGlam = (Proto.PetGlam)g.ReturnSummonGlam,
            AttunementCount = g.AttunementCount,
            AttunementType = (Proto.SummonAttunement)g.AttunementType,
            AetherFlags = (uint)g.AetherFlags,
            IsBahamutReady = g.IsBahamutReady,
            IsPhoenixReady = g.IsPhoenixReady,
            IsIfritReady = g.IsIfritReady,
            IsTitanReady = g.IsTitanReady,
            IsGarudaReady = g.IsGarudaReady,
            IsIfritAttuned = g.IsIfritAttuned,
            IsTitanAttuned = g.IsTitanAttuned,
            IsGarudaAttuned = g.IsGarudaAttuned,
            HasAetherflowStacks = g.HasAetherflowStacks,
            AetherflowStacks = g.AetherflowStacks,
        };

        public static Proto.ViperGauge MapViper(VPRGauge g) => new()
        {
            RattlingCoilStacks = g.RattlingCoilStacks,
            SerpentOffering = g.SerpentOffering,
            AnguineTribute = g.AnguineTribute,
            DreadCombo = (Proto.DreadCombo)g.DreadCombo,
            SerpentCombo = (Proto.SerpentCombo)g.SerpentCombo,
        };

        public static Proto.WarriorGauge MapWarrior(WARGauge g) => new()
        {
            BeastGauge = g.BeastGauge,
        };

        public static Proto.WhiteMageGauge MapWhiteMage(WHMGauge g) => new()
        {
            LilyTimer = g.LilyTimer,
            Lily = g.Lily,
            BloodLily = g.BloodLily,
        };
    }
}
