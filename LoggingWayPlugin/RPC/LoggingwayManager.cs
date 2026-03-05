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

        private long? _currentReportId;
        private DateTime _reportIdLastRefreshTime = DateTime.MinValue;
        public LoggingwayManager(LoggingwayClientWrapper clientWrapper)
        {
            _clientWrapper = clientWrapper;
        }

        public async Task RefreshReportId()
        {
                       if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot refresh report ID when not logged in.");
                return;
            }
            if(_currentReportId is null || (_reportIdLastRefreshTime - DateTime.Now).TotalHours > 6 )
            {
                try
                {
                    _currentReportId = await _clientWrapper.CreateNewReportAsync(0);
                    Service.Log.Debug($"New report obtained with ID: {_currentReportId}");
                    _reportIdLastRefreshTime = DateTime.Now;
                }
                catch (Exception ex) { Service.Log.Error($"Error creating new report: {ex.Message}"); return; }
                
                return;
            }
            

        }
        //SubmitCombatEvents will be unused for now,Encounter expect
        //the list to begin with EncounterStart and end with EncounterEnd
        //so basically wholesale Encounters
        public async Task<uint> SubmitEncounter(IEnumerable<CombatEvent> events)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot submit combat events when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            await RefreshReportId();
            if (_currentReportId is null)
            {
                Service.Log.Warning("Cannot submit combat events without a report ID.");
                throw new InvalidOperationException("No report ID");
            }
            return await _clientWrapper.EncounterIngestAsync(_currentReportId!.Value, events);
        }
        /*public async Task<uint> SubmitCombatEvents(IAsyncEnumerable<CombatEvent> events)
        {
            if (LoginState != LoggingwayLoginState.LoggedIn)
            {
                Service.Log.Warning("Cannot submit combat events when not logged in.");
                throw new InvalidOperationException("Not logged in");
            }
            if(_currentReportId is null)
            {
                Service.Log.Warning("Cannot submit combat events without a report ID.");
                throw new InvalidOperationException("No report ID");
            }
            //return await _clientWrapper.CombatEventIngestAsync(_currentReportId!.Value, events);
        }*/
        public void StartLoginProcedure()
        {
            LoginState = LoggingwayLoginState.LoggingIn;
            _clientWrapper.GetXivAuthRedirectAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    LoginException = $"Error getting auth redirect: {task.Exception?.Message}";
                    Service.Log.Error($"Error getting auth redirect: {task.Exception?.Message}");
                    LoginState = LoggingwayLoginState.LoggingError;
                    return;
                }
                var redirectUri = task.Result;
                // Open the redirect URI in the user's default browser
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = redirectUri,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LoginException = $"Error opening browser: {ex.Message}";
                    Service.Log.Error($"Error opening browser: {ex.Message}");
                    LoginState = LoggingwayLoginState.LoggingError;
                }
            }).ContinueWith(async task => {
                await LocalCallbackServer.WaitForCallbackAsync().ContinueWith(async callbackTask =>
                {
                    if (callbackTask.IsFaulted)
                    {
                        LoginException = $"Error waiting for callback: {callbackTask.Exception?.Message}";
                        Service.Log.Error($"Error waiting for callback: {callbackTask.Exception?.Message}");
                        LoginState = LoggingwayLoginState.LoggingError;
                        return;
                    }
                    var (code, state) = callbackTask.Result;
                    try
                    {
                        await _clientWrapper.LoginAsync(code, state);
                        LoginException = "";
                        Service.Log.Debug("Login successful!");
                        LoginState = LoggingwayLoginState.LoggedIn;
                    }
                    catch (Exception ex)
                    {
                        LoginException = $"Error during login: {ex.Message}";
                        Service.Log.Error($"Error during login: {ex.Message}");
                        LoginState = LoggingwayLoginState.LoggingError;
                    }
                });



            });
        }


    }
}

public enum LoggingwayLoginState
{
    NotLoggedIn,
    LoggingIn,
    LoggingError,
    LoggedIn
}
