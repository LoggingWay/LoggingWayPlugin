using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using SamplePlugin.Loggingway;
using SamplePlugin.Proto;
namespace SamplePlugin.RPC
{             
    public sealed class LoggingwayClientWrapper : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly Loggingway.Loggingway.LoggingwayClient _client;

        //sessionID for now stored only in memory
        //TODO:idk find something to save it
        private string? _sessionID;

        public LoggingwayClientWrapper(string grpcEndpoint)
        {
            _channel = GrpcChannel.ForAddress(grpcEndpoint);
            _client = new Loggingway.Loggingway.LoggingwayClient(_channel);
        }

        public void Dispose()
        {
            ClearSessionID();//eventually will need to tell the server to clear out the session but for now w/e
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

        public async Task<long> CreateNewReportAsync(uint visibility, CancellationToken ct = default)
        {
            EnsureAuthenticated();

            try
            {
                var reply = await _client.CreateNewReportAsync(
                    new NewReportRequest { Visbility = visibility },
                    CreateAuthHeaders(),
                    cancellationToken: ct);

                return reply.Reportid;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        public async Task<uint> EncounterIngestAsync(long reportId,IEnumerable<CombatEvent> events, CancellationToken ct = default)
        {
            EnsureAuthenticated();
            var headers = CreateAuthHeaders();
            try
            {
                var reply = await _client.EncounterIngestAsync(
                    new NewEncounterRequest { ReportId = reportId,Events = { events } },
                    headers,
                    cancellationToken: ct);
                return reply.Code;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }
        public async Task<uint> CombatEventIngestAsync(
            string reportId,
            IAsyncEnumerable<CombatEvent> events,
            CancellationToken ct = default)
        {
            EnsureAuthenticated();

            var headers = CreateAuthHeaders();
            headers.Add("reportid", reportId);

            try
            {
                using var call = _client.CombatEventIngest(headers, cancellationToken: ct);

                await foreach (var ev in events.WithCancellation(ct))
                {
                    await call.RequestStream.WriteAsync(ev);
                }

                await call.RequestStream.CompleteAsync();

                var response = await call;
                return response.Code;
            }
            catch (RpcException ex)
            {
                throw TranslateRpcException(ex);
            }
        }

        // ============================
        // JWT HANDLING
        // ============================

        private void StoreSessionID(string sessionID)
        {
            ClearSessionID();
            _sessionID = sessionID;
        }

        private void ClearSessionID()
        {
            if (_sessionID != null)
            {
                _sessionID = null;
            }
        }

        private void EnsureAuthenticated()
        {
            if (_sessionID == null)
                throw new InvalidOperationException("Client is not authenticated.");
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
