using System;

namespace LippoLand.Soap.Client
{
    /// <summary>
    /// Entry point: fires 5 SOAP operations against WS_OnlineBooking,
    /// each wrapped in an Instana exit span with soap.action tagged.
    ///
    /// Real service:
    ///   URL:       https://connect.lippoland.id/InternalMobileAppsService/WS_OnlineBooking.asmx
    ///   Namespace: http://tempuri.org/
    ///   Auth:      SOAP header AuthHeader (domainName, userName, password)
    ///
    /// Configuration via environment variables (override at runtime):
    ///   LIPPO_SERVICE_URL   — default: http://localhost:8080/OnlineBooking
    ///   LIPPO_AUTH_DOMAIN   — default: LIPPOLAND
    ///   LIPPO_AUTH_USER     — default: demo_user
    ///   LIPPO_AUTH_PASS     — default: demo_pass
    ///   INSTANA_AGENT_URL   — default: http://localhost:42699
    /// </summary>
    class Program
    {
        private const string BASE_ACTION = "http://tempuri.org/";

        static void Main(string[] args)
        {
            // Read config from environment (easy to override in Docker / VM)
            string serviceUrl  = Env("LIPPO_SERVICE_URL",  "http://localhost:8080/OnlineBooking");
            string authDomain  = Env("LIPPO_AUTH_DOMAIN",  "LIPPOLAND");
            string authUser    = Env("LIPPO_AUTH_USER",    "demo_user");
            string authPass    = Env("LIPPO_AUTH_PASS",    "demo_pass");
            string instanaUrl  = Env("INSTANA_AGENT_URL",  "http://localhost:42699");

            Console.WriteLine("=================================================");
            Console.WriteLine(" LippoLand WS_OnlineBooking — Instana Tracing Demo");
            Console.WriteLine(" Target  : {0}", serviceUrl);
            Console.WriteLine(" Instana : {0}", instanaUrl);
            Console.WriteLine("=================================================\n");

            var client = new SoapHttpClient(serviceUrl,
                                            new System.Net.Http.HttpClient(),
                                            new System.Net.Http.HttpClient(),
                                            instanaUrl);

            // ── Call 1: retrieveComponentDiagramatic ─────────────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "retrieveComponentDiagramatic",
                soapEnvelope: SoapEnvelopes.retrieveComponentDiagramatic(
                    json:       "{\"projectCode\":\"LP-JKT\",\"clusterCode\":\"CL-01\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "retrieveComponentDiagramatic");

            // ── Call 2: retrieveAvailableUnitForOnlineBooking ────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "retrieveAvailableUnitForOnlineBooking",
                soapEnvelope: SoapEnvelopes.retrieveAvailableUnitForOnlineBooking(
                    json:       "{\"projectCode\":\"LP-JKT\",\"clusterCode\":\"CL-01\",\"unitType\":\"2BR\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "retrieveAvailableUnitForOnlineBooking");

            // ── Call 3: ReserveSelectedUnit ──────────────────────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "ReserveSelectedUnit",
                soapEnvelope: SoapEnvelopes.ReserveSelectedUnit(
                    json:       "{\"unitCode\":\"U-2BR-012\",\"memberCode\":\"MBR-001\",\"projectCode\":\"LP-JKT\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "ReserveSelectedUnit");

            // ── Call 4: CalculateSellingPriceUnit ────────────────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "CalculateSellingPriceUnit",
                soapEnvelope: SoapEnvelopes.CalculateSellingPriceUnit(
                    json:       "{\"unitCode\":\"U-2BR-012\",\"termNo\":\"T-001\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "CalculateSellingPriceUnit");

            // ── Call 5: BookingUnit ──────────────────────────────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "BookingUnit",
                soapEnvelope: SoapEnvelopes.BookingUnit(
                    json:       "{\"unitCode\":\"U-2BR-012\",\"memberCode\":\"MBR-001\",\"projectCode\":\"LP-JKT\",\"termNo\":\"T-001\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "BookingUnit");

            Console.WriteLine("\n=================================================");
            Console.WriteLine(" All calls complete. Check Instana for traces.");
            Console.WriteLine(" Filter: soap.action = \"{0}BookingUnit\"", BASE_ACTION);
            Console.WriteLine("=================================================");
        }

        private static void RunCall(SoapHttpClient client, string soapAction,
                                    string soapEnvelope, string label)
        {
            Console.WriteLine("── {0} ──", label);
            Console.WriteLine("   SOAPAction : {0}", soapAction);
            try
            {
                string response = client.Call(soapAction, soapEnvelope);
                Console.WriteLine("   Response   : {0}\n",
                    response.Length > 200 ? response.Substring(0, 200) + "..." : response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ERROR      : {0}\n", ex.Message);
            }
        }

        private static string Env(string key, string fallback)
            => Environment.GetEnvironmentVariable(key) ?? fallback;
    }
}
