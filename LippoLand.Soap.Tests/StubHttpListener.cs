using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Minimal in-process HTTP listener used as a SOAP stub backend in tests.
    /// Returns the given response body to every POST and optionally captures
    /// received headers via the onRequest callback.
    /// Dispose() stops the listener cleanly.
    /// </summary>
    public sealed class StubHttpListener : IDisposable
    {
        private readonly HttpListener                  _listener;
        private readonly string                        _responseBody;
        private readonly Action<NameValueCollection>   _onRequest;
        private readonly Thread                        _thread;
        private          int                           _disposed; // 0=running, 1=disposed

        public StubHttpListener(string prefix, string responseBody,
            Action<NameValueCollection> onRequest = null)
        {
            _responseBody = responseBody;
            _onRequest    = onRequest;
            _listener     = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();

            _thread = new Thread(Serve) { IsBackground = true };
            _thread.Start();
        }

        private void Serve()
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = _listener.GetContext();
                }
                catch (HttpListenerException) { return; } // listener stopped
                catch (ObjectDisposedException) { return; }
                catch (InvalidOperationException) { return; }

                // Capture headers before writing response (callback may be slow)
                _onRequest?.Invoke(ctx.Request.Headers);

                try
                {
                    byte[] body = Encoding.UTF8.GetBytes(_responseBody);
                    ctx.Response.ContentType     = "text/xml; charset=utf-8";
                    ctx.Response.ContentLength64 = body.Length;
                    ctx.Response.StatusCode      = 200;
                    ctx.Response.OutputStream.Write(body, 0, body.Length);
                    ctx.Response.OutputStream.Close();
                }
                catch { /* ignore write errors on disposed/closed responses */ }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                try { _listener.Stop(); } catch { }
                try { _listener.Close(); } catch { }
            }
        }
    }
}
