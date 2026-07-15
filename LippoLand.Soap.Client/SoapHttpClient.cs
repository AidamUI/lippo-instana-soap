using System;
using System.Net;
using System.IO;
using System.Text;
using Instana.ManagedTracing.Sdk;
using Instana.ManagedTracing.Api;

namespace LippoLand.Soap.Client
{
    /// <summary>
    /// Wraps every outbound SOAP call in an Instana exit span.
    ///
    /// KEY POINT FOR LIPPOLAND:
    ///   Instana auto-traces the raw HTTP POST but it does NOT read the
    ///   SOAPAction header by default. By calling span.SetTag("soap.action", soapAction)
    ///   we make the SOAPAction appear as a filterable/searchable attribute in
    ///   Instana's Unbounded Analytics and Call Details view.
    ///   This lets you build filters like:  soap.action = "SubmitBooking"
    /// </summary>
    public class SoapHttpClient
    {
        private readonly string _endpointUrl;

        public SoapHttpClient(string endpointUrl)
        {
            _endpointUrl = endpointUrl;
        }

        /// <summary>
        /// Executes a raw SOAP HTTP call and wraps it in an Instana exit span.
        /// The SOAPAction is tagged so it becomes a filterable parameter in Instana.
        /// </summary>
        /// <param name="soapAction">
        ///   Value for the SOAPAction HTTP header, e.g.
        ///   "http://tempuri.org/BookingUnit"
        /// </param>
        /// <param name="soapEnvelope">Raw SOAP XML body.</param>
        /// <returns>Raw SOAP XML response string.</returns>
        public string Call(string soapAction, string soapEnvelope)
        {
            string response = null;
            string operationName = ExtractOperationName(soapAction);

            // ── Instana exit span ─────────────────────────────────────────────
            // CreateExit marks this as a downstream call in the trace tree.
            // The Action<string,string> overload lets us inject trace headers
            // into outgoing HTTP headers via an HttpWebRequest wrapper.
            //
            // We cannot pass AddTag directly to CreateExit here because
            // HttpWebRequest.Headers is our propagation carrier, so we use a
            // two-step approach:
            //   1. Create exit span, propagate trace context via custom header bag
            //   2. Tag the SOAPAction on the span for Instana filtering
            // ─────────────────────────────────────────────────────────────────
            var headerBag = new System.Collections.Generic.Dictionary<string, string>();

            using (var span = CustomSpan.CreateExit(this, (key, value) => headerBag[key] = value))
            {
                // ── Tag SOAPAction — THIS IS THE FILTER PARAMETER ─────────────
                // In Instana UI: Unbounded Analytics → filter by  soap.action
                span.SetTag("soap.action",     soapAction);
                span.SetTag("soap.operation",  operationName);
                span.SetTag("soap.endpoint",   _endpointUrl);
                span.SetServiceName("LippoLand-OnlineBooking");
                span.SetEndpointName(operationName);
                // ─────────────────────────────────────────────────────────────

                response = span.Wrap<string>(() =>
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpointUrl);
                    request.Method      = "POST";
                    request.ContentType = "text/xml; charset=utf-8";

                    // ── SOAPAction header (required for SOAP 1.1) ─────────────
                    request.Headers.Add("SOAPAction", "\"" + soapAction + "\"");

                    // ── Propagate Instana trace context headers ────────────────
                    foreach (var kv in headerBag)
                        request.Headers.Add(kv.Key, kv.Value);

                    byte[] bodyBytes = Encoding.UTF8.GetBytes(soapEnvelope);
                    request.ContentLength = bodyBytes.Length;

                    using (Stream reqStream = request.GetRequestStream())
                        reqStream.Write(bodyBytes, 0, bodyBytes.Length);

                    using (HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse())
                    using (Stream resStream = httpResponse.GetResponseStream())
                    using (StreamReader reader = new StreamReader(resStream, Encoding.UTF8))
                        return reader.ReadToEnd();

                }, true); // true = rethrow exceptions (captured in span automatically)
            }

            return response;
        }

        /// <summary>
        /// Extracts a short operation name from a full SOAPAction URI.
        /// e.g. ".../InternalMobileAppsService/SubmitBooking" → "SubmitBooking"
        /// </summary>
        private static string ExtractOperationName(string soapAction)
        {
            if (string.IsNullOrEmpty(soapAction)) return "UnknownOperation";
            int lastSlash = soapAction.LastIndexOf('/');
            return lastSlash >= 0 ? soapAction.Substring(lastSlash + 1) : soapAction;
        }
    }
}
