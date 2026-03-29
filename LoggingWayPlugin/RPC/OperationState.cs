using LoggingWayPlugin.Proto;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoggingWayPlugin.RPC
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationStatus
    {
        Idle,
        Loading,
        Success,
        Error
    }

    public class OperationState<T>
    {
        [JsonProperty] public OperationStatus Status { get; private set; } = OperationStatus.Idle;
        [JsonProperty] public T? Data { get; private set; }
        [JsonIgnore] public Exception? Error { get; private set; }
        [JsonProperty] public DateTime? LastUpdated { get; private set; }

        [JsonIgnore] public bool IsLoading => Status == OperationStatus.Loading;
        [JsonIgnore] public bool IsSuccess => Status == OperationStatus.Success;
        [JsonIgnore] public bool IsError => Status == OperationStatus.Error;

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
