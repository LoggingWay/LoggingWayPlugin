using LoggingWayPlugin.Proto;
using LoggingWayPlugin.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Windows
{
    internal class MainView
    {
        public OperationState<IReadOnlyList<Character>> Characters { get; } = new();
        public OperationState<IReadOnlyList<Encounter>> Encounters { get; } = new();
        public OperationState<EncounterPlayerBreakdown> Breakdown { get; } = new();

        public OperationState<IReadOnlyList<LeaderBoardEntry>> Leaderboard { get; } = new();

        private readonly LoggingwayManager loggingwayManager;

        public MainView(LoggingwayManager manager)
        {
            loggingwayManager = manager;
        }

        public async void RefreshCharacters()
        {
            await RunOperation(Characters, async () =>
            {
                var reply = await loggingwayManager.GetCharacters();
                return (IReadOnlyList<Character>)reply.Characters.ToList();
            });
        }

        public async void RefreshEncounters(uint zoneId)
        {
            await RunOperation(Encounters, async () =>
            {
                var reply = await loggingwayManager.GetMyEncounters(zoneId);
                return (IReadOnlyList<Encounter>)reply.Encounters.ToList();
            });
        }

        public async void FindEncounterBreakdown(long encounterId)
        {
            await RunOperation(Breakdown, async () =>
            {
                var reply = await loggingwayManager.GetEncounterStats(encounterId);
                return reply.Playerstats;
            });
        }

        public async void RefreshLeaderBoard(uint cfcId)
        {
            await RunOperation(Leaderboard, async () =>
            {
                var reply = await loggingwayManager.GetLeaderBoard(cfcId);
                return (IReadOnlyList<LeaderBoardEntry>)reply.Entry.ToList();
            });
        }

        public async void RefreshLeaderBoard(uint cfcId, uint jobId)
        {
            await RunOperation(Leaderboard, async () =>
            {
                var reply = await loggingwayManager.GetLeaderBoard(cfcId, jobId);
                return (IReadOnlyList<LeaderBoardEntry>)reply.Entry.ToList();
            });
        }

        private static async Task RunOperation<T>(
            OperationState<T> state,
            Func<Task<T>> operation)
        {
            if (state.IsLoading) return;

            try
            {
                state.SetLoading();
                var result = await operation();
                state.SetSuccess(result);
            }
            catch (Exception ex)
            {
                state.SetError(ex);
            }
        }
    }
}
