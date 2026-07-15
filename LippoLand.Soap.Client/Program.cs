using System;
using System.Configuration;

namespace LippoLand.Soap.Client
{
    /// <summary>
    /// Entry point demonstrating real WS_OnlineBooking operations with Instana tracing.
    ///
    /// Real service facts (from live WSDL scrape):
    ///   - URL:       https://connect.lippoland.id/InternalMobileAppsService/WS_OnlineBooking.asmx
    ///   - Namespace: http://tempuri.org/
    ///   - Auth:      SOAP header AuthHeader (domainName, userName, password)
    ///   - Params:    all ops take a single JSON string
    ///   - Returns:   all ops return a single JSON string
    ///
    /// Credentials are read from App.config — never hardcoded.
    /// </summary>
    class Program
    {
        // Defaults for local stub; override in App.config for real backend
        private const string DEFAULT_SERVICE_URL = "http://localhost:8080/OnlineBooking";
        private const string BASE_ACTION         = "http://tempuri.org/";

        static void Main(string[] args)
        {
            string serviceUrl  = ConfigurationManager.AppSettings["ServiceUrl"]   ?? DEFAULT_SERVICE_URL;
            string authDomain  = ConfigurationManager.AppSettings["AuthDomain"]   ?? "LIPPOLAND";
            string authUser    = ConfigurationManager.AppSettings["AuthUser"]     ?? "demo_user";
            string authPass    = ConfigurationManager.AppSettings["AuthPassword"] ?? "demo_pass";

            Console.WriteLine("=================================================");
            Console.WriteLine(" LippoLand WS_OnlineBooking — Instana Tracing Demo");
            Console.WriteLine(" Target: {0}", serviceUrl);
            Console.WriteLine("=================================================\n");

            var client = new SoapHttpClient(serviceUrl);

            // ── Call 1: retrieveComponentDiagramatic (friend's operation) ─────
            RunCall(client,
                soapAction:   BASE_ACTION + "retrieveComponentDiagramatic",
                soapEnvelope: SoapEnvelopes.retrieveComponentDiagramatic(
                    json:       "{\"projectCode\":\"LP-JKT\",\"clusterCode\":\"CL-01\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "retrieveComponentDiagramatic");

            // ── Call 2: retrieveAvailableUnitForOnlineBooking ─────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "retrieveAvailableUnitForOnlineBooking",
                soapEnvelope: SoapEnvelopes.retrieveAvailableUnitForOnlineBooking(
                    json:       "{\"projectCode\":\"LP-JKT\",\"clusterCode\":\"CL-01\",\"unitType\":\"2BR\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "retrieveAvailableUnitForOnlineBooking");

            // ── Call 3: ReserveSelectedUnit ───────────────────────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "ReserveSelectedUnit",
                soapEnvelope: SoapEnvelopes.ReserveSelectedUnit(
                    json:       "{\"unitCode\":\"U-2BR-012\",\"memberCode\":\"MBR-001\",\"projectCode\":\"LP-JKT\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "ReserveSelectedUnit");

            // ── Call 4: CalculateSellingPriceUnit ─────────────────────────────
            RunCall(client,
                soapAction:   BASE_ACTION + "CalculateSellingPriceUnit",
                soapEnvelope: SoapEnvelopes.CalculateSellingPriceUnit(
                    json:       "{\"unitCode\":\"U-2BR-012\",\"termNo\":\"T-001\"}",
                    authDomain: authDomain, authUser: authUser, authPass: authPass),
                label: "CalculateSellingPriceUnit");

            // ── Call 5: BookingUnit ───────────────────────────────────────────
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
            Console.ReadLine();
        }

        private static void RunCall(SoapHttpClient client, string soapAction, string soapEnvelope, string label)
        {
            Console.WriteLine("── {0} ──", label);
            Console.WriteLine("   SOAPAction : {0}", soapAction);
            try
            {
                string response = client.Call(soapAction, soapEnvelope);
                Console.WriteLine("   Response   : {0}\n",
                    response.Length > 300 ? response.Substring(0, 300) + "..." : response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   ERROR      : {0}\n", ex.Message);
            }
        }
    }
}
