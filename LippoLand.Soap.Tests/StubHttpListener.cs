using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Minimal in-process HTTP listener used as a SOAP stub backend in tests.
    /// Starts on the given prefix, returns the given response body to every POST,
    /// and optionally captures received headers via the onRequest callback.
    /// Dispose() stops the listener.
    /// </summary>
    public sealed class StubHttpListener : IDisposable
    {
        private readonly HttpListener       _listener;
        private readonly string             _responseBody;
        private readonly Action<NameValueCollection> _onRequest;
        private readonly Thread             _thread;
        private volatile bool               _running = true;

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
            while (_running)
            {
                HttpListenerContext ctx;
                try { ctx = _listener.GetContext(); }
                catch { return; } // listener stopped

                if (_onRequest != null) _onRequest(ctx.Request.Headers);

                byte[] body = Encoding.UTF8.GetBytes(_responseBody);
                ctx.Response.ContentType     = "text/xml; charset=utf-8";
                ctx.Response.ContentLength64 = body.Length;
                ctx.Response.StatusCode      = 200;
                ctx.Response.OutputStream.Write(body, 0, body.Length);
                ctx.Response.OutputStream.Close();
            }
        }

        public void Dispose()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
        }
    }
}
