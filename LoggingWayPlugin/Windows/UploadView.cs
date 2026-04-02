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

            // The server long-polls for 10s, then returns ready:false if the job isn't done. In that case we immediately retry until we get ready:true or an error/timeout.
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
                    // Server returned ready:false (timed out its 10s wait) — retry immediately
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    State.SetFailed("Job expired or not found.");
                    return;
                }
                catch (Exception ex)
                {
                    State.SetFailed(ex.Message);
                    return;
                }
            }
        }

        public void Dispose()
        {
            _pollCts?.Cancel();
            _pollCts?.Dispose();
        }
    }
}
