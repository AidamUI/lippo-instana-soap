using System;
using System.Net.Http;
using System.Text;
using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using LippoLand.Soap.Client;
using LippoLand.Soap.Service;

namespace LippoLand.Soap.Monolith
{
    /// <summary>
    /// THE MONOLITH SCENARIO.
    ///
    /// This single process acts as BOTH:
    ///   - The SOAP server  (CoreWCF on /soap)
    ///   - The SOAP caller  (triggered via two HTTP endpoints)
    ///
    /// This mirrors LippoLand's real architecture where one module inside the
    /// monolith calls another module via SOAP over localhost.
    ///
    /// TWO TRIGGER ENDPOINTS:
    ///
    ///   GET /trigger/with
    ///     Uses SoapHttpClient (our instrumented wrapper).
    ///     → Instana sees: named exit span, soap.action tag, dependency link
    ///       in the service map, filterable in Unbounded Analytics.
    ///
    ///   GET /trigger/without
    ///     Uses a plain HttpClient with zero instrumentation.
    ///     → Instana sees: nothing (or at best an anonymous HTTP POST with no
    ///       SOAPAction, no service name, not linked to any trace).
    ///
    /// HOW TO RUN:
    ///   docker run --rm --network host -v $(pwd):/src -w /src \
    ///     -e INSTANA_AGENT_URL=http://localhost:42699 \
    ///     mcr.microsoft.com/dotnet/sdk:8.0 \
    ///     dotnet run --project LippoLand.Soap.Monolith
    ///
    /// THEN TRIGGER EACH SCENARIO:
    ///   curl http://localhost:8090/trigger/with
    ///   curl http://localhost:8090/trigger/without
    /// </summary>
    class Program
    {
        // The monolith calls ITSELF on this address
        private const string SELF_SOAP_URL = "http://localhost:8090/soap";
        private const string BASE_ACTION   = "http://tempuri.org/";
        private const string AUTH_DOMAIN   = "LIPPOLAND";
        private const string AUTH_USER     = "demo_user";
        private const string AUTH_PASS     = "demo_pass";

        static void Main(string[] args)
        {
            string instanaUrl = Environment.GetEnvironmentVariable("INSTANA_AGENT_URL")
                                ?? "http://localhost:42699";

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://0.0.0.0:8090");

            // CoreWCF services
            builder.Services.AddServiceModelServices();
            builder.Services.AddServiceModelMetadata();
            builder.Services.AddSingleton<OnlineBookingService>();

            var app = builder.Build();

            // ── SOAP service endpoint ─────────────────────────────────────────
            app.UseServiceModel(svc =>
            {
                svc.AddService<OnlineBookingService>(opts =>
                {
                    opts.BaseAddresses.Add(new Uri(SELF_SOAP_URL));
                });
                svc.AddServiceEndpoint<OnlineBookingService, IOnlineBookingService>(
                    new BasicHttpBinding { MaxReceivedMessageSize = 10 * 1024 * 1024 }, "");
            });

            var smb = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
            smb.HttpGetEnabled = true;

            // ── Trigger: WITH instrumentation ─────────────────────────────────
            // This is what LippoLand SHOULD look like after the SDK integration.
            // The monolith calls itself via SOAP — Instana sees the full dependency.
            app.MapGet("/trigger/with", (HttpContext ctx) =>
            {
                string scenario = "WITH Instana instrumentation";
                Console.WriteLine("\n============ SCENARIO A: {0} ============", scenario);

                var client = new SoapHttpClient(SELF_SOAP_URL,
                    new HttpClient(),
                    new HttpClient(),
                    instanaUrl);

                var results = new System.Text.StringBuilder();
                results.AppendLine($"Scenario: {scenario}");
                results.AppendLine($"Calling self at: {SELF_SOAP_URL}");
                results.AppendLine();

                // Call 1: retrieveAvailableUnitForOnlineBooking
                string action1 = BASE_ACTION + "retrieveAvailableUnitForOnlineBooking";
                Console.WriteLine("[WITH] Calling {0}", action1);
                try
                {
                    string r1 = client.Call(action1,
                        SoapEnvelopes.retrieveAvailableUnitForOnlineBooking(
                            "{\"projectCode\":\"LP-JKT\"}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS));
                    results.AppendLine($"[1] retrieveAvailableUnitForOnlineBooking => {Trim(r1)}");
                    Console.WriteLine("[WITH] OK: {0}", Trim(r1));
                }
                catch (Exception ex) { results.AppendLine($"[1] ERROR: {ex.Message}"); }

                // Call 2: BookingUnit  (the CRITICAL operation for LippoLand)
                string action2 = BASE_ACTION + "BookingUnit";
                Console.WriteLine("[WITH] Calling {0}", action2);
                try
                {
                    string r2 = client.Call(action2,
                        SoapEnvelopes.BookingUnit(
                            "{\"unitCode\":\"U-2BR-012\",\"memberCode\":\"MBR-001\"," +
                            "\"projectCode\":\"LP-JKT\",\"termNo\":\"T-001\"}",
                            AUTH_DOMAIN, AUTH_USER, AUTH_PASS));
                    results.AppendLine($"[2] BookingUnit => {Trim(r2)}");
                    Console.WriteLine("[WITH] OK: {0}", Trim(r2));
                }
                catch (Exception ex) { results.AppendLine($"[2] ERROR: {ex.Message}"); }

                results.AppendLine();
                results.AppendLine("Instana: exit spans reported to agent.");
                results.AppendLine($"  Filter in Analytics: soap.action = \"{BASE_ACTION}BookingUnit\"");
                results.AppendLine("  Check: Infrastructure > Services > LippoLand-OnlineBooking");

                Console.WriteLine("[WITH] Done. Check Instana Analytics for spans.");
                return Results.Text(results.ToString());
            });

            // ── Trigger: WITHOUT instrumentation ─────────────────────────────
            // This is what LippoLand looks like TODAY — no SDK, no span, no tags.
            // Instana will see a raw HTTP POST at best, with no SOAPAction visible.
            app.MapGet("/trigger/without", (HttpContext ctx) =>
            {
                string scenario = "WITHOUT Instana instrumentation (baseline)";
                Console.WriteLine("\n============ SCENARIO B: {0} ============", scenario);

                // Plain HttpClient — exactly what LippoLand does today.
                // No span opened. No SOAPAction tagged. No trace context propagated.
                var http = new HttpClient();

                var results = new System.Text.StringBuilder();
                results.AppendLine($"Scenario: {scenario}");
                results.AppendLine($"Calling self at: {SELF_SOAP_URL}");
                results.AppendLine();

                // Call 1: retrieveAvailableUnitForOnlineBooking  (no instrumentation)
                string action1 = BASE_ACTION + "retrieveAvailableUnitForOnlineBooking";
                Console.WriteLine("[WITHOUT] Calling {0}", action1);
                try
                {
                    string r1 = RawSoapPost(http, SELF_SOAP_URL, action1,
                        SoapEnvelopes.retrieveAvailableUnitForOnlineBooking(
                            "{\"projectCode\":\"LP-JKT\"}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS));
                    results.AppendLine($"[1] retrieveAvailableUnitForOnlineBooking => {Trim(r1)}");
                    Console.WriteLine("[WITHOUT] OK: {0}", Trim(r1));
                }
                catch (Exception ex) { results.AppendLine($"[1] ERROR: {ex.Message}"); }

                // Call 2: BookingUnit  (no instrumentation)
                string action2 = BASE_ACTION + "BookingUnit";
                Console.WriteLine("[WITHOUT] Calling {0}", action2);
                try
                {
                    string r2 = RawSoapPost(http, SELF_SOAP_URL, action2,
                        SoapEnvelopes.BookingUnit(
                            "{\"unitCode\":\"U-2BR-012\",\"memberCode\":\"MBR-001\"," +
                            "\"projectCode\":\"LP-JKT\",\"termNo\":\"T-001\"}",
                            AUTH_DOMAIN, AUTH_USER, AUTH_PASS));
                    results.AppendLine($"[2] BookingUnit => {Trim(r2)}");
                    Console.WriteLine("[WITHOUT] OK: {0}", Trim(r2));
                }
                catch (Exception ex) { results.AppendLine($"[2] ERROR: {ex.Message}"); }

                results.AppendLine();
                results.AppendLine("Instana: NO spans reported. Agent sees only raw HTTP POST.");
                results.AppendLine("  In Instana Analytics: no soap.action filter possible.");
                results.AppendLine("  In Service Map: no dependency link for this SOAP call.");

                Console.WriteLine("[WITHOUT] Done. Instana has no trace for this call.");
                return Results.Text(results.ToString());
            });

            // ── Info page ─────────────────────────────────────────────────────
            app.MapGet("/", () => Results.Text(
                "LippoLand Monolith Demo\n\n" +
                "Endpoints:\n" +
                "  GET /trigger/with     → SOAP self-call WITH Instana instrumentation\n" +
                "  GET /trigger/without  → SOAP self-call WITHOUT instrumentation\n" +
                "  GET /soap?wsdl        → WSDL for the hosted SOAP service\n\n" +
                "Run both, then compare in Instana:\n" +
                "  Analytics > Calls > filter: soap.action = \"http://tempuri.org/BookingUnit\"\n" +
                "  Infrastructure > Services  (look for LippoLand-OnlineBooking)\n"
            ));

            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║  LippoLand Monolith Demo  —  running on :8090        ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════╣");
            Console.WriteLine("║  SOAP service : http://localhost:8090/soap           ║");
            Console.WriteLine("║  WSDL         : http://localhost:8090/soap?wsdl      ║");
            Console.WriteLine("║  Scenario A   : http://localhost:8090/trigger/with   ║");
            Console.WriteLine("║  Scenario B   : http://localhost:8090/trigger/without║");
            Console.WriteLine("║  Instana URL  : {0,-38}║", instanaUrl);
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            app.Run();
        }

        /// <summary>
        /// Bare SOAP POST — no Instana spans, no trace headers.
        /// This is exactly what a monolith does when it has NO instrumentation.
        /// </summary>
        private static string RawSoapPost(HttpClient http, string url,
                                           string soapAction, string envelope)
        {
            var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "\"" + soapAction + "\"");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            // NOTE: No X-INSTANA-T, no X-INSTANA-S — trace context is NOT propagated.

            var response = http.Send(request);
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        private static string Trim(string s)
            => s != null && s.Length > 120 ? s.Substring(0, 120) + "..." : s ?? "";
    }
}
