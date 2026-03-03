using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SamplePlugin.RPC;
public static class LocalCallbackServer
{
    public static async Task<(string code, string state)> WaitForCallbackAsync(
        int port = 6767,
        CancellationToken ct = default)
    {
        var prefix = $"http://localhost:{port}/";

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
                Service.Log.Error("Received callback without code or state parameters.");

            // Respond to browser
            var responseString = "Login successful. You can close this window.";
            var buffer = Encoding.UTF8.GetBytes(responseString);

            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();

            return (code!, state!);
        }
        finally
        {
            listener.Stop();
        }
    }
}
