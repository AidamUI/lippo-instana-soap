using System;
using System.Collections.Generic;

/// <summary>
/// Minimal stub of Instana.ManagedTracing.Sdk so the test project compiles
/// without the NuGet package installed. The stub records what was called so
/// SoapHttpClientTests can assert on span behaviour.
/// </summary>

namespace Instana.ManagedTracing.Api
{
    public class DistributedTraceInformation
    {
        public long ParentSpanId { get; set; }
        public long TraceId      { get; set; }
    }
}

namespace Instana.ManagedTracing.Sdk
{
    using Instana.ManagedTracing.Api;

    public class CustomSpan : IDisposable
    {
        // ── Recorded calls for test assertions ───────────────────────────────
        public static readonly List<string> TagsSet            = new List<string>();
        public static readonly List<string> ServiceNamesSet    = new List<string>();
        public static readonly List<string> EndpointNamesSet   = new List<string>();
        public static Action<string, string> LastPropagationCb = null;

        public static void Reset()
        {
            TagsSet.Clear();
            ServiceNamesSet.Clear();
            EndpointNamesSet.Clear();
            LastPropagationCb = null;
        }

        // ── SDK API surface ───────────────────────────────────────────────────

        public static CustomSpan Create()
        {
            return new CustomSpan();
        }

        public static CustomSpan CreateEntry()
        {
            return new CustomSpan();
        }

        public static CustomSpan CreateExit(object owner, Action<string, string> propagationCallback)
        {
            LastPropagationCb = propagationCallback;
            // Simulate the SDK writing trace context headers
            propagationCallback("X-INSTANA-T", "deadbeef00000001");
            propagationCallback("X-INSTANA-S", "deadbeef00000002");
            return new CustomSpan();
        }

        public void SetTag(string key, string value)
        {
            TagsSet.Add(key + "=" + value);
        }

        public void SetServiceName(string name)
        {
            ServiceNamesSet.Add(name);
        }

        public void SetEndpointName(string name)
        {
            EndpointNamesSet.Add(name);
        }

        public void SetData(string key, string value) { }
        public void SetResult(string result)           { }
        public void SetError(Exception ex)             { }

        public T Wrap<T>(Func<T> func, bool rethrow = true)
        {
            try   { return func(); }
            catch { if (rethrow) throw; return default(T); }
        }

        public void WrapAction(Action action, bool rethrow = true)
        {
            try   { action(); }
            catch { if (rethrow) throw; }
        }

        public void Dispose() { }
    }
}
