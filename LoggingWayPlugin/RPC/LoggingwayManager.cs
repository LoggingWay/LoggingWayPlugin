using LoggingWayPlugin;
using LoggingWayPlugin.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.RPC
{
    public class LoggingwayManager
    {
        private readonly LoggingwayClientWrapper _clientWrapper;
        public LoggingwayLoginState LoginState { get; private set; } = LoggingwayLoginState.NotLoggedIn;
        public string LoginException { get; private set; } = "";

        public LoggingwayManager(LoggingwayClientWrapper clientWrapper)
        {
            _clientWrapper = clientWrapper;
            if (_clientWrapper.HasSession) {
                LoginState = LoggingwayLoginState.LoggedIn;
            }
        }

        public async Task<uint> SubmitEncounter(IEnumerable<CombatEvent> events)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot submit combat events when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            return await _clientWrapper.EncounterIngestAsync(events);
        }
        public async Task StartLoginProcedureAsync(CancellationToken ct = default)
        {
            LoginState = LoggingwayLoginState.LoggingIn;
            LoginException = string.Empty;

            try
            {
                var redirectUri = await _clientWrapper.GetXivAuthRedirectAsync(ct);

                OpenBrowser(redirectUri);

                var (code, state) = await LocalCallbackServer.Instance.WaitForCallbackAsync(ct);

                await _clientWrapper.LoginAsync(code, state, ct);

                Service.Log.Debug("Login successful!");
                LoginState = LoggingwayLoginState.LoggedIn;
            }
            catch (OperationCanceledException)
            {
                LoginState = LoggingwayLoginState.LoggingError;
                LoginException = "Login was cancelled.";
                Service.Log.Warning("Login procedure was cancelled.");
            }
            catch (InvalidOperationException ex)
            {
                Service.Log.Warning($"Callback listener already active: {ex.Message}");
            }
            catch (Exception ex)
            {
                LoginState = LoggingwayLoginState.LoggingError;
                LoginException = ex.Message;
                Service.Log.Error($"Login error: {ex.Message}");
            }
        }

        private void OpenBrowser(string uri)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error opening browser: {ex.Message}", ex);
            }
        }

        public async Task<GetMyCharactersReply>GetCharacters()
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot get character info when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            try
            {
                return await _clientWrapper.GetMyCharacters();
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error getting character info: {ex.Message}");
                throw;
            }
        }

        public async Task<GetEncountersStatsReply> GetEncounterStats(long encounterId)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot get encounter stats when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            try
            {
                var reply = await _clientWrapper.GetEncounterStats(encounterId);
                return reply;
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error getting encounter stats: {ex.Message}");
                throw;
            }
        }

        public async Task<GetMyEncountersReply> GetMyEncounters(uint zoneId)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot get encounters when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            try
            {
                var reply = await _clientWrapper.GetMyEncounters(zoneId);
                return reply;
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error getting encounters: {ex.Message}");
                throw;
            }
        }

        public async Task<GetLeaderBoardReply> GetLeaderBoard(uint zoneId)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot get leaderboard when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            try
            {
                var reply = await _clientWrapper.GetLeaderBoard(zoneId);
                return reply;
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error getting leaderboard: {ex.Message}");
                throw;
            }
        }

        public async Task<GetLeaderBoardReply> GetLeaderBoard(uint zoneId, uint jobId)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot get leaderboard when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            try
            {
                var reply = await _clientWrapper.GetLeaderBoard(zoneId, jobId);
                return reply;
            }
            catch (Exception ex)
            {
                Service.Log.Error($"Error getting leaderboard: {ex.Message}");
                throw;
            }
        }

        public async Task Logout()
        {
            await _clientWrapper.Logout();
            LoginState = LoggingwayLoginState.NotLoggedIn;
        }
    }

    public enum LoggingwayLoginState
    {
        NotLoggedIn,
        LoggingIn,
        LoggingError,
        LoggedIn
    }
}
