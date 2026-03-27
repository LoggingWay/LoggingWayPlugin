using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace LoggingWayPlugin.RPC;

public sealed class LocalCallbackServer : IDisposable
{
    private static readonly Lazy<LocalCallbackServer> _instance = new(() => new LocalCallbackServer());
    public static LocalCallbackServer Instance => _instance.Value;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isListening;
    private const int Port = 6767;

    private LocalCallbackServer() { }

    public async Task<(string Code, string State)> WaitForCallbackAsync(CancellationToken ct = default)
    {
        // Prevent multiple simultaneous listeners
        if (!await _lock.WaitAsync(0, ct))
            throw new InvalidOperationException("A callback listener is already active.");//this will be caught in login procedure

        _isListening = true;
        var prefix = $"http://localhost:{Port}/";

        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        Service.Log.Debug($"Listening for OAuth callback on {prefix}");

        try
        {
            var context = await listener.GetContextAsync().WaitAsync(ct);
            var query = HttpUtility.ParseQueryString(context.Request.Url!.Query);

            var code = query["code"];
            var state = query["state"];

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                throw new InvalidOperationException("Received callback without code or state parameters.");

            await RespondToBrowserAsync(context);

            return (code!, state!);
        }
        finally
        {
            listener.Stop();
            _isListening = false;
            _lock.Release();
        }
    }

    private static async Task RespondToBrowserAsync(HttpListenerContext context)
    {
        const string responseString = "Login successful. You can close this window.";
        var buffer = Encoding.UTF8.GetBytes(responseString);

        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
