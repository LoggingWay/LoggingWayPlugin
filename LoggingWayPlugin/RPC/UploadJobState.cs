using LoggingWayPlugin.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.RPC
{
    public enum UploadPhase { Idle, Queued, Polling, Done, Failed }

    public class UploadJobState
    {
        public UploadPhase Phase { get; private set; } = UploadPhase.Idle;
        public string? JobId { get; private set; }
        public DateTimeOffset? QueuedAt { get; private set; }
        public PollJobResultReply? Result { get; private set; }
        public string? ErrorMessage { get; private set; }

        public bool IsTerminal => Phase is UploadPhase.Done or UploadPhase.Failed;

        internal void SetQueued(string jobId, DateTimeOffset queuedAt)
        {
            JobId = jobId;
            QueuedAt = queuedAt;
            Phase = UploadPhase.Queued;
        }

        internal void SetPolling() => Phase = UploadPhase.Polling;

        internal void SetDone(PollJobResultReply result)
        {
            Result = result;
            Phase = UploadPhase.Done;
        }

        internal void SetFailed(string error)
        {
            ErrorMessage = error;
            Phase = UploadPhase.Failed;
        }
    }
}
