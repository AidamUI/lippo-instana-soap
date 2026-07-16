using System;
using System.Net;
using System.Net.Http;
using LippoLand.Soap.Client;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Unit tests for SoapHttpClient.
    ///
    /// Tests assert on:
    ///   1.  ExtractOperationName: extracts last path segment of SOAPAction URI
    ///   2.  SOAPAction header is double-quoted per SOAP 1.1 spec
    ///   3.  Instana agent receives a span with soap.action, soap.operation, soap.endpoint
    ///   4.  X-INSTANA-T / X-INSTANA-S propagation headers are sent to the SOAP target
    ///   5.  Response body is returned correctly
    ///   6.  Exceptions are rethrown
    ///
    /// HTTP is tested with an in-process HttpListener stub.
    /// The Instana agent calls are intercepted by InstanaHandlerStub.
    /// </summary>
    public static class SoapHttpClientTests
    {
        public static void Run()
        {
            Test_ExtractOperationName_FullUri();
            Test_ExtractOperationName_TrailingSlash();
            Test_ExtractOperationName_Empty();
            Test_ExtractOperationName_Null();
            Test_SpanContains_SoapActionTag();
            Test_SpanContains_SoapOperationTag();
            Test_SpanContains_SoapEndpointTag();
            Test_PropagationHeaders_Sent_To_SoapTarget();
            Test_SOAPAction_Header_IsDoubleQuoted();
            Test_ResponseBody_Returned();
            Test_Exception_Rethrown();
            Console.WriteLine("  [SoapHttpClientTests] All passed.");
        }

        // ── ExtractOperationName (public static — can be called directly) ─────

        static void Test_ExtractOperationName_FullUri()
        {
            string result = SoapHttpClient.ExtractOperationName("http://tempuri.org/BookingUnit");
            Assert.AreEqual("BookingUnit", result, "Expected BookingUnit");
        }

        static void Test_ExtractOperationName_TrailingSlash()
        {
            // Trailing slash → substring after last '/' is ""
            string result = SoapHttpClient.ExtractOperationName("http://tempuri.org/");
            Assert.AreEqual("", result, "Trailing slash should yield empty string");
        }

        static void Test_ExtractOperationName_Empty()
        {
            Assert.AreEqual("UnknownOperation",
                SoapHttpClient.ExtractOperationName(""), "Empty → UnknownOperation");
        }

        static void Test_ExtractOperationName_Null()
        {
            Assert.AreEqual("UnknownOperation",
                SoapHttpClient.ExtractOperationName(null), "Null → UnknownOperation");
        }

        // ── Span tag content ─────────────────────────────────────────────────

        static void Test_SpanContains_SoapActionTag()
        {
            var (_, instanaStub, client) = MakeClient("http://localhost:18080/Test/");
            using (var listener = new StubHttpListener("http://localhost:18080/Test/", "<r/>"))
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");

            bool found = instanaStub.Requests.Exists(r =>
                r.Body.Contains("\"soap.action\"") &&
                r.Body.Contains("BookingUnit"));
            Assert.IsTrue(found,
                "Instana span should contain soap.action=BookingUnit. Got:\n" +
                string.Join("\n", instanaStub.Requests.ConvertAll(r => r.Body)));
        }

        static void Test_SpanContains_SoapOperationTag()
        {
            var (_, instanaStub, client) = MakeClient("http://localhost:18081/Test/");
            using (var listener = new StubHttpListener("http://localhost:18081/Test/", "<r/>"))
                client.Call("http://tempuri.org/ReserveSelectedUnit", "<soap/>");

            bool found = instanaStub.Requests.Exists(r =>
                r.Body.Contains("\"soap.operation\"") &&
                r.Body.Contains("ReserveSelectedUnit"));
            Assert.IsTrue(found, "Instana span should contain soap.operation=ReserveSelectedUnit");
        }

        static void Test_SpanContains_SoapEndpointTag()
        {
            var (_, instanaStub, client) = MakeClient("http://localhost:18082/Test/");
            using (var listener = new StubHttpListener("http://localhost:18082/Test/", "<r/>"))
                client.Call("http://tempuri.org/VerifyPayment", "<soap/>");

            bool found = instanaStub.Requests.Exists(r =>
                r.Body.Contains("\"soap.endpoint\"") &&
                r.Body.Contains("18082"));
            Assert.IsTrue(found, "Instana span should contain soap.endpoint with port 18082");
        }

        // ── Trace propagation headers ─────────────────────────────────────────

        static void Test_PropagationHeaders_Sent_To_SoapTarget()
        {
            string capturedTraceId = null;
            string capturedSpanId  = null;

            // Use a real StubHttpListener so we can capture the received headers
            using (var listener = new StubHttpListener("http://localhost:18083/Test/",
                "<r/>",
                headers => {
                    capturedTraceId = headers["X-INSTANA-T"];
                    capturedSpanId  = headers["X-INSTANA-S"];
                }))
            {
                var (_, _, client) = MakeClient("http://localhost:18083/Test/");
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");
            }

            // The client sets X-INSTANA-T / X-INSTANA-S from the spanId/traceId it generates
            Assert.IsNotNullOrEmpty(capturedTraceId, "X-INSTANA-T should be forwarded to SOAP target");
            Assert.IsNotNullOrEmpty(capturedSpanId,  "X-INSTANA-S should be forwarded to SOAP target");
        }

        // ── HTTP wire behaviour ───────────────────────────────────────────────

        static void Test_SOAPAction_Header_IsDoubleQuoted()
        {
            string capturedSoapAction = null;
            using (var listener = new StubHttpListener("http://localhost:18084/Test/",
                "<r/>",
                headers => { capturedSoapAction = headers["SOAPAction"]; }))
            {
                var (_, _, client) = MakeClient("http://localhost:18084/Test/");
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");
            }
            // SOAP 1.1 spec: SOAPAction must be double-quoted
            Assert.AreEqual("\"http://tempuri.org/BookingUnit\"", capturedSoapAction,
                "SOAPAction header must be double-quoted per SOAP 1.1 spec");
        }

        static void Test_ResponseBody_Returned()
        {
            string expected = "<soap:Envelope><soap:Body><result>OK</result></soap:Body></soap:Envelope>";
            using (var listener = new StubHttpListener("http://localhost:18085/Test/", expected))
            {
                var (_, _, client) = MakeClient("http://localhost:18085/Test/");
                string actual = client.Call("http://tempuri.org/BookingUnit", "<soap/>");
                Assert.AreEqual(expected, actual, "Response body not returned correctly");
            }
        }

        static void Test_Exception_Rethrown()
        {
            // Port 19999 — nothing listening → HttpRequestException
            var (_, _, client) = MakeClient("http://localhost:19999/NoService/");
            bool threw = false;
            try   { client.Call("http://tempuri.org/BookingUnit", "<soap/>"); }
            catch { threw = true; }
            Assert.IsTrue(threw, "Expected exception to be rethrown on network error");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a SoapHttpClient with:
        ///   - real HttpClient for the SOAP call (hits the StubHttpListener)
        ///   - InstanaHandlerStub for the agent calls (intercepted in-memory)
        /// Returns (soapHttp, instanaStub, client).
        /// </summary>
        private static (HttpClient soapHttp, InstanaHandlerStub instanaStub, SoapHttpClient client)
            MakeClient(string endpointUrl)
        {
            var soapHttp    = new HttpClient();
            var instanaStub = new InstanaHandlerStub();
            var instanaHttp = new HttpClient(instanaStub);
            var client      = new SoapHttpClient(endpointUrl, soapHttp, instanaHttp,
                                                 "http://localhost:9999"); // fake agent URL
            return (soapHttp, instanaStub, client);
        }
    }
}
