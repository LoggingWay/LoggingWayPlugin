using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using Grpc.Core;
using Grpc.Net.Client;
using LoggingWayPlugin.Proto;
namespace LoggingWayPlugin.RPC
{             
    public sealed class LoggingwayClientWrapper : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly Loggingway.LoggingwayClient _client;
        private Configuration _configuration;

        //sessionID is persisted in config,will add some secret later
        //this mean someone can theoretically copy it from file but if they have unrestricted access to the file system they can do way worse things than just posting fake encounters soooooo
        private string _sessionID;
        private DateTime _sessionExpirationDate;

        public bool HasSession => !_sessionID.IsNullOrEmpty() && DateTime.UtcNow < _sessionExpirationDate;
        public LoggingwayClientWrapper(string grpcEndpoint,Configuration config)
        {
            _channel = GrpcChannel.ForAddress(grpcEndpoint);
            _client = new Loggingway.LoggingwayClient(_channel);
            _configuration = config;
            _sessionID = config.LastSessionId;
            _sessionExpirationDate = config.SessionExpirationDate;
            if (!HasSession)
            {
                Service.NotificationManager.AddNotification(new Notification
                {
                    Title = "LoggingWay",
                    Content = "Your session has expired, please log in again",
                    Type = NotificationType.Warning
                });
                _sessionID = string.Empty;
                _sessionExpirationDate = DateTime.MinValue;
            }
            else
            {
                SessionRefreshSync();
            }
        }

        public void Dispose()
        {
            _channel.Dispose();
        }

        // ============================
        // AUTH-FREE CALLS
        // ============================

        public async Task<string> GetXivAuthRedirectAsync(CancellationToken ct = default)
        {
            try
            {
                var reply = await _client.GetXivAuthRedirectAsync(
                    new GetXivAuthRedirectRequest(),
                    cancellationToken: ct);

                return reply.Xivauthuri;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task LoginAsync(string code, string state, CancellationToken ct = default)
        {
            try
            {
                var reply = await _client.LoginAsync(new LoginRequest
                {
                    Code = code,
                    State = state
                }, cancellationToken: ct);
                if (reply.Code != 0)
                {
                    Service.Log.Warning("No characters found while checking XivAuth");
                    Service.NotificationManager.AddNotification(new Notification
                    {
                        Title = "LoggingWay",
                        Content = "No Characters where found associated with your XivAuth account,do not forget to add characters via the associated characters tab",
                        Type = NotificationType.Warning
                    });
                }
                StoreSessionID(reply.SessionID);
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        // ============================
        // AUTH REQUIRED CALLS
        // ============================
        //Any calls below this line require a valid sessionID or will be auto-rejected by the server
        // ============================
        // SERVICE RELATED CALLS    
        // ============================
        private async void SessionRefreshAsync(CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.SessionRefreshAsync(
                    new SessionRefreshRequest(),
                    headers,
                    cancellationToken: ct);
                StoreSessionID(reply.SessionID);
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        //fuck it I'll figure it out later
        //ONLY USE THIS IN THE CONSTRUCTOR
        private void SessionRefreshSync()
        {
            //EnsureAuthenticated();
            if (_channel.State != ConnectivityState.Ready)
            {
                _sessionID = string.Empty;
                _sessionExpirationDate = DateTime.MinValue;
                return;//dont hang up if the server isn't even reachable
            }
            var headers = CreateAuthHeaders();
            try
            {
                var reply = _client.SessionRefresh(
                    new SessionRefreshRequest(),
                    headers,
                    cancellationToken: CancellationToken.None);
                StoreSessionID(reply.SessionID);
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task Logout(CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                await _client.LogoutAsync(
                    new LogoutRequest(),
                    headers,
                    cancellationToken: ct);
                _sessionID = "";
                _configuration.LastSessionId = "";
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task<EnrollCharactersReply> EnrollCharactersAsync(string code, string state, CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.EnrollCharactersAsync(
                    new EnrollCharactersRequest { Code= code, State= state },
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }
        // ============================
        // GAME/DATA RELATED CALLS
        // ============================
        public async Task<NewEncounterReply> EncounterIngestAsync(IEnumerable<CombatEvent> events, uint cfcid, CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.EncounterIngestAsync(
                    new NewEncounterRequest { Events = { events }, CfcId = cfcid },
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task<GetMyCharactersReply> GetMyCharacters(CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.GetMyCharactersAsync(
                    new GetMyCharactersRequest(),
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        //if zoneid is 0 it will return 20 encounters across all zones, otherwise it will filter by the provided zoneid
        public async Task<GetMyEncountersReply> GetMyEncounters(uint cfcid,CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.GetMyEncountersAsync(
                    new GetMyEncountersRequest() { CfcId = cfcid },
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task<GetEncountersStatsReply> GetEncounterStats(long encounterid,CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.GetEncountersStatsAsync(
                    new GetEncountersStatsRequest() { EncounterId = encounterid},
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task<GetLeaderBoardReply> GetLeaderBoard(uint cfcid,CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.GetLeaderBoardAsync(
                    new GetLeaderBoardRequest() { CfcId = cfcid },
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task<GetLeaderBoardReply> GetLeaderBoard(uint cfcid,uint jobid,CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.GetLeaderBoardAsync(
                    new GetLeaderBoardRequest() { CfcId = cfcid, JobId = jobid },
                    headers,
                    cancellationToken: ct);
                return reply;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        // ============================
        // SESSIONID HANDLING
        // ============================

        private void StoreSessionID(string sessionID)
        {
            _sessionID = sessionID;
            _sessionExpirationDate = DateTime.UtcNow.AddDays(1);//session is valid for 24 hours serverside
            _configuration.LastSessionId = sessionID;
            _configuration.SessionExpirationDate = _sessionExpirationDate;
            _configuration.Save();
        }

        private void EnsureAuthenticated()
        {
            if (_sessionID.IsNullOrEmpty())
                throw new InvalidOperationException("Client is not authenticated.");
            if (DateTime.UtcNow >= _sessionExpirationDate)
            {
                _sessionID = "";
                _configuration.LastSessionId = "";
                throw new InvalidOperationException("Session has expired. Please log in again.");
            }
            if (DateTime.UtcNow >= _sessionExpirationDate.AddHours(1))//if session is expiring in less than 1 hour
            {
                SessionRefreshAsync();
            }
        }
        private Metadata CreateAuthHeaders()
        {
            EnsureAuthenticated();

            return new Metadata
        {
            { "authorization", $"{_sessionID}" }
        };
        }

        private Exception TranslateRpcException(RpcException ex)
        {
            return ex.StatusCode switch
            {

                _ => ex
            };
        }
    }

}
