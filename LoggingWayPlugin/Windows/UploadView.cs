using Grpc.Core;
using LoggingWayPlugin.Proto;
using LoggingWayPlugin.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.Windows
{
    public class UploadView : IDisposable
    {
        public UploadJobState State { get; } = new();

        private readonly LoggingwayManager manager;
        private readonly ResultWindow _resultWindow;
        private readonly Configuration _config;
        private CancellationTokenSource? _pollCts;

        public UploadView(LoggingwayManager manager,ResultWindow resultWindow,Configuration config)
        {
            this.manager = manager;
            this._resultWindow = resultWindow;
            this._config = config;
        }

        public void BeginPolling(string jobId)
        {
            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource();
            _ = PollLoop(jobId, _pollCts.Token);
            if (_config.AutoOpenResultWindow)
            {
                _resultWindow.IsOpen = true;
            }

        }

        private async Task PollLoop(string jobId, CancellationToken ct)
        {
            State.SetPolling();
            var backoff = TimeSpan.FromMilliseconds(500);
            var maxBackoff = TimeSpan.FromSeconds(15);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await manager.PollJobResult(jobId, ct);

                    if (result.Ready)
                    {
                        State.SetDone(result);
                        return;
                    }

                    backoff = TimeSpan.FromMilliseconds(500);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    State.SetFailed("Job expired or not found.");
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode is StatusCode.Unauthenticated
                                                             or StatusCode.PermissionDenied)
                {
                    State.SetFailed(ex.Status.Detail);
                    return;
                }
                catch (RpcException ex) when (IsTransient(ex.StatusCode))
                {
                    try { await Task.Delay(backoff, ct); } catch (OperationCanceledException) { return; }
                    backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, maxBackoff.Ticks));
                }
                catch (Exception ex)
                {
                    State.SetFailed(ex.Message);
                    return;
                }
            }
        }

        static bool IsTransient(StatusCode c) =>
            c is StatusCode.Unavailable or StatusCode.DeadlineExceeded;

        public void Dispose()
        {
            _pollCts?.Cancel();
            _pollCts?.Dispose();
        }
    }
}
