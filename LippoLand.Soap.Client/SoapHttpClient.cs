using System;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace LippoLand.Soap.Client
{
    /// <summary>
    /// Wraps every outbound SOAP call in an Instana exit span via the
    /// Instana agent's local REST SDK (http://localhost:42699).
    ///
    /// WHY NOT THE NUGET SDK?
    ///   Instana.ManagedTracing.Sdk only ships net45 binaries and does not
    ///   run on .NET 8 / Linux. The agent REST API is the supported alternative
    ///   for modern runtimes and produces identical results in the UI.
    ///   Docs: https://www.ibm.com/docs/en/instana-observability?topic=features-net-tracing-sdk
    ///
    /// KEY POINT FOR LIPPOLAND:
    ///   Instana auto-traces the raw HTTP POST but does NOT read the SOAPAction
    ///   header by default. By posting a custom span to the agent with tag
    ///   "soap.action" = the SOAPAction value, that attribute becomes filterable
    ///   in Instana's Unbounded Analytics:  soap.action = "BookingUnit"
    ///
    /// SPAN FLOW:
    ///   1. POST /com.instana.plugin.generic.trace  → open exit span, get spanId
    ///   2. HTTP SOAP call with X-INSTANA-T / X-INSTANA-S propagation headers
    ///   3. PATCH /com.instana.plugin.generic.trace/{spanId} → close span with duration + tags
    /// </summary>
    public class SoapHttpClient
    {
        private readonly string     _endpointUrl;
        private readonly HttpClient _http;
        private readonly HttpClient _instana;
        private readonly string     _agentUrl;

        private const string DEFAULT_AGENT = "http://localhost:42699";

        public SoapHttpClient(string endpointUrl)
            : this(endpointUrl, new HttpClient(), new HttpClient(), DEFAULT_AGENT) { }

        /// <summary>Injectable constructor for unit tests.</summary>
        public SoapHttpClient(string endpointUrl, HttpClient http,
                              HttpClient instanaHttp = null, string agentUrl = DEFAULT_AGENT)
        {
            _endpointUrl = endpointUrl;
            _http        = http;
            _instana     = instanaHttp ?? new HttpClient();
            _agentUrl    = agentUrl ?? DEFAULT_AGENT;
        }

        /// <summary>
        /// Executes a raw SOAP HTTP call wrapped in an Instana exit span.
        /// The SOAPAction is tagged so it becomes filterable in Instana.
        /// </summary>
        public string Call(string soapAction, string soapEnvelope)
        {
            string operationName = ExtractOperationName(soapAction);
            var    sw            = System.Diagnostics.Stopwatch.StartNew();
            // Generate trace IDs upfront so we can propagate them in headers
            string spanId        = NewId();
            string traceId       = NewId();
            bool   hasError      = false;
            string errorMessage  = null;
            long   timestamp     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string response = null;
            try
            {
                // ── SOAP HTTP call ────────────────────────────────────────────
                var content = new StringContent(soapEnvelope ?? "", Encoding.UTF8, "text/xml");

                // SOAPAction: double-quoted per SOAP 1.1 spec
                content.Headers.Add("SOAPAction", "\"" + (soapAction ?? "") + "\"");

                var request = new HttpRequestMessage(HttpMethod.Post, _endpointUrl)
                {
                    Content = content
                };

                // Propagate trace context so LippoLand's backend can continue the trace
                request.Headers.TryAddWithoutValidation("X-INSTANA-T", traceId);
                request.Headers.TryAddWithoutValidation("X-INSTANA-S", spanId);

                var httpResponse = _http.Send(request);
                httpResponse.EnsureSuccessStatusCode();
                response = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                hasError     = true;
                errorMessage = ex.Message;
                throw;
            }
            finally
            {
                sw.Stop();
                // ── Report single complete span to agent (duration required) ──
                ReportSpan(spanId, traceId, operationName, soapAction,
                           timestamp, sw.ElapsedMilliseconds, hasError, errorMessage);
            }

            return response;
        }

        // ── Instana agent REST helper ─────────────────────────────────────────

        private void ReportSpan(string spanId, string traceId, string operationName,
                                 string soapAction, long timestamp, long durationMs,
                                 bool hasError, string errorMessage)
        {
            try
            {
                // tags{} → searchable in Instana Unbounded Analytics (SetTag equivalent)
                // data{} → service/endpoint mapping, not searchable
                // error must be explicit — omitting it causes Instana to infer errors
                string body = string.Format(
                    "{{\"spanId\":\"{0}\",\"traceId\":\"{1}\"," +
                    "\"type\":\"EXIT\",\"name\":\"soap.call\"," +
                    "\"timestamp\":{2}," +
                    "\"duration\":{3}," +
                    "\"error\":{6}," +
                    "\"tags\":{{" +
                    "\"soap.action\":\"{5}\"," +
                    "\"soap.operation\":\"{4}\"" +
                    "}}," +
                    "\"data\":{{" +
                    "\"service\":\"LippoLand-OnlineBooking\"," +
                    "\"endpoint\":\"{4}\"{7}" +
                    "}}}}",
                    spanId, traceId,
                    timestamp,
                    durationMs,
                    Esc(operationName),
                    Esc(soapAction ?? ""),
                    hasError ? "true" : "false",
                    hasError ? string.Format(",\"errorMessage\":\"{0}\"", Esc(errorMessage)) : "");

                var req = new HttpRequestMessage(HttpMethod.Post,
                    _agentUrl + "/com.instana.plugin.generic.trace")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                _instana.Send(req);
            }
            catch { /* agent unreachable — degrade gracefully, never break the SOAP call */ }
        }

        private static string NewId()
            => Guid.NewGuid().ToString("N").Substring(0, 16);

        private static string Esc(string s)
            => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        /// <summary>
        /// Extracts a short operation name from a full SOAPAction URI.
        /// e.g. "http://tempuri.org/BookingUnit" → "BookingUnit"
        /// </summary>
        public static string ExtractOperationName(string soapAction)
        {
            if (string.IsNullOrEmpty(soapAction)) return "UnknownOperation";
            int lastSlash = soapAction.LastIndexOf('/');
            return lastSlash >= 0 ? soapAction.Substring(lastSlash + 1) : soapAction;
        }
    }
}
