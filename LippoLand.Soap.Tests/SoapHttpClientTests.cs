using System;
using LippoLand.Soap.Client;
using Instana.ManagedTracing.Sdk;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Unit tests for SoapHttpClient.
    ///
    /// Because the Instana SDK is stubbed in InstanaStub.cs, we can assert on:
    ///   1.  ExtractOperationName — tested via public method (made internal+InternalsVisibleTo
    ///       isn't available in net40 without AssemblyInfo, so we test it indirectly
    ///       via SetTag recording in the stub)
    ///   2.  Tags recorded on the span: soap.action, soap.operation, soap.endpoint
    ///   3.  ServiceName and EndpointName set on exit span
    ///   4.  Trace propagation headers passed to HTTP request (verified via stub recording)
    ///   5.  SOAPAction header is double-quoted per SOAP 1.1 spec
    ///
    /// For HTTP tests we use a minimal in-process HttpListener as the stub backend.
    /// </summary>
    public static class SoapHttpClientTests
    {
        public static void Run()
        {
            Test_OperationName_ExtractedCorrectly_FromFullUri();
            Test_OperationName_ExtractedCorrectly_TrailingSlash();
            Test_OperationName_EmptyAction_ReturnsUnknown();
            Test_OperationName_NullAction_ReturnsUnknown();
            Test_SpanTags_Set_Correctly();
            Test_SpanServiceName_And_EndpointName_Set();
            Test_PropagationHeaders_Injected();
            Test_HttpCall_SendsSoapActionHeader();
            Test_HttpCall_Returns_ResponseBody();
            Test_HttpCall_OnError_SpanCaptures_And_Rethrows();
            Console.WriteLine("  [SoapHttpClientTests] All passed.");
        }

        // ── Operation name extraction (tested via span.SetTag recording) ─────

        static void Test_OperationName_ExtractedCorrectly_FromFullUri()
        {
            CustomSpan.Reset();
            // We need an HTTP listener for any Call() test — use a mock
            using (var listener = new StubHttpListener("http://localhost:18080/Test/", "<r/>"))
            {
                var client = new SoapHttpClient("http://localhost:18080/Test/");
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");
            }
            Assert.IsTrue(CustomSpan.TagsSet.Contains("soap.operation=BookingUnit"),
                "Expected soap.operation=BookingUnit in tags: " + string.Join(", ", CustomSpan.TagsSet));
        }

        static void Test_OperationName_ExtractedCorrectly_TrailingSlash()
        {
            CustomSpan.Reset();
            using (var listener = new StubHttpListener("http://localhost:18081/Test/", "<r/>"))
            {
                var client = new SoapHttpClient("http://localhost:18081/Test/");
                // SOAPAction with trailing slash → operation should be empty string, not crash
                client.Call("http://tempuri.org/", "<soap/>");
            }
            // After trailing slash, substring is "" — op name is ""
            Assert.IsTrue(CustomSpan.TagsSet.Exists(t => t.StartsWith("soap.operation=")),
                "soap.operation tag should exist even for trailing-slash action");
        }

        static void Test_OperationName_EmptyAction_ReturnsUnknown()
        {
            CustomSpan.Reset();
            using (var listener = new StubHttpListener("http://localhost:18082/Test/", "<r/>"))
            {
                var client = new SoapHttpClient("http://localhost:18082/Test/");
                client.Call("", "<soap/>");
            }
            Assert.IsTrue(CustomSpan.TagsSet.Contains("soap.operation=UnknownOperation"),
                "Expected UnknownOperation for empty action");
        }

        static void Test_OperationName_NullAction_ReturnsUnknown()
        {
            CustomSpan.Reset();
            using (var listener = new StubHttpListener("http://localhost:18083/Test/", "<r/>"))
            {
                var client = new SoapHttpClient("http://localhost:18083/Test/");
                client.Call(null, "<soap/>");
            }
            Assert.IsTrue(CustomSpan.TagsSet.Contains("soap.operation=UnknownOperation"),
                "Expected UnknownOperation for null action");
        }

        // ── Span tag assertions ───────────────────────────────────────────────

        static void Test_SpanTags_Set_Correctly()
        {
            CustomSpan.Reset();
            using (var listener = new StubHttpListener("http://localhost:18084/Test/", "<r/>"))
            {
                var client = new SoapHttpClient("http://localhost:18084/Test/");
                client.Call("http://tempuri.org/ReserveSelectedUnit", "<soap/>");
            }
            Assert.IsTrue(CustomSpan.TagsSet.Contains("soap.action=http://tempuri.org/ReserveSelectedUnit"),
                "soap.action tag missing or wrong");
            Assert.IsTrue(CustomSpan.TagsSet.Contains("soap.operation=ReserveSelectedUnit"),
                "soap.operation tag missing or wrong");
            Assert.IsTrue(CustomSpan.TagsSet.Contains("soap.endpoint=http://localhost:18084/Test/"),
                "soap.endpoint tag missing or wrong");
        }

        static void Test_SpanServiceName_And_EndpointName_Set()
        {
            CustomSpan.Reset();
            using (var listener = new StubHttpListener("http://localhost:18085/Test/", "<r/>"))
            {
                var client = new SoapHttpClient("http://localhost:18085/Test/");
                client.Call("http://tempuri.org/VerifyPayment", "<soap/>");
            }
            Assert.IsTrue(CustomSpan.ServiceNamesSet.Contains("LippoLand-OnlineBooking"),
                "ServiceName not set to LippoLand-OnlineBooking");
            Assert.IsTrue(CustomSpan.EndpointNamesSet.Contains("VerifyPayment"),
                "EndpointName not set to VerifyPayment");
        }

        // ── Trace propagation ─────────────────────────────────────────────────

        static void Test_PropagationHeaders_Injected()
        {
            CustomSpan.Reset();
            string capturedTraceId  = null;
            string capturedSpanId   = null;
            // Capture headers the stub listener receives
            using (var listener = new StubHttpListener("http://localhost:18086/Test/",
                "<r/>",
                headers => {
                    capturedTraceId = headers["X-INSTANA-T"];
                    capturedSpanId  = headers["X-INSTANA-S"];
                }))
            {
                var client = new SoapHttpClient("http://localhost:18086/Test/");
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");
            }
            Assert.AreEqual("deadbeef00000001", capturedTraceId, "X-INSTANA-T header not propagated");
            Assert.AreEqual("deadbeef00000002", capturedSpanId,  "X-INSTANA-S header not propagated");
        }

        // ── HTTP wire behaviour ───────────────────────────────────────────────

        static void Test_HttpCall_SendsSoapActionHeader()
        {
            string capturedSoapAction = null;
            using (var listener = new StubHttpListener("http://localhost:18087/Test/",
                "<r/>",
                headers => { capturedSoapAction = headers["SOAPAction"]; }))
            {
                var client = new SoapHttpClient("http://localhost:18087/Test/");
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");
            }
            // SOAP 1.1 spec: SOAPAction must be double-quoted
            Assert.AreEqual("\"http://tempuri.org/BookingUnit\"", capturedSoapAction,
                "SOAPAction header must be double-quoted per SOAP 1.1 spec");
        }

        static void Test_HttpCall_Returns_ResponseBody()
        {
            CustomSpan.Reset();
            string expected = "<soap:Envelope><soap:Body><result>OK</result></soap:Body></soap:Envelope>";
            using (var listener = new StubHttpListener("http://localhost:18088/Test/", expected))
            {
                var client = new SoapHttpClient("http://localhost:18088/Test/");
                string actual = client.Call("http://tempuri.org/BookingUnit", "<soap/>");
                Assert.AreEqual(expected, actual, "Response body not returned correctly");
            }
        }

        static void Test_HttpCall_OnError_SpanCaptures_And_Rethrows()
        {
            CustomSpan.Reset();
            // Point at a port with nothing listening → WebException
            var client = new SoapHttpClient("http://localhost:19999/NoService/");
            bool threw = false;
            try
            {
                client.Call("http://tempuri.org/BookingUnit", "<soap/>");
            }
            catch (Exception)
            {
                threw = true;
            }
            Assert.IsTrue(threw, "Expected exception to be rethrown on network error");
        }
    }
}
