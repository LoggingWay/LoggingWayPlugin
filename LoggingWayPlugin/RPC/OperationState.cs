using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LoggingWayPlugin.RPC
{
    public enum OperationStatus
    {
        Idle,
        Loading,
        Success,
        Error
    }

    public class OperationState<T>
    {
        [JsonInclude] public OperationStatus Status { get; private set; } = OperationStatus.Idle;
        [JsonInclude] public T? Data { get; private set; }
        [JsonInclude] public Exception? Error { get; private set; }
        [JsonInclude] public DateTime? LastUpdated { get; private set; }

        public bool IsLoading => Status == OperationStatus.Loading;
        public bool IsSuccess => Status == OperationStatus.Success;
        public bool IsError => Status == OperationStatus.Error;

        internal void SetLoading()
        {
            Status = OperationStatus.Loading;
            Error = null;
        }

        internal void SetSuccess(T data)
        {
            Data = data;
            Status = OperationStatus.Success;
            LastUpdated = DateTime.UtcNow;
        }

        internal void SetError(Exception ex)
        {
            Error = ex;
            Status = OperationStatus.Error;
            LastUpdated = DateTime.UtcNow;
        }
    }
}
