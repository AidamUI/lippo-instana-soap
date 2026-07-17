using System;

namespace LippoLand.Soap.Service
{
    /// <summary>
    /// Stub implementation of the real WS_OnlineBooking contract.
    ///
    /// PRODUCTION IMITATION:
    ///   The real LippoLand ASMX code wraps each [WebMethod] body with
    ///   CustomSpan.Create() from Instana.ManagedTracing.Sdk:
    ///
    ///     [WebMethod]
    ///     public DataSet GetComponentDiagramatic(string projectcode, string clustercode)
    ///     {
    ///         using (var span = CustomSpan.Create())
    ///         {
    ///             span.SetTag("soapAction", "GetComponentDiagramatic");
    ///             span.WrapAction(() => {
    ///                 // DB calls, business logic
    ///             }, true);
    ///         }
    ///     }
    ///
    ///   We do the same here with InstanaSpan.Create() — our REST API equivalent.
    ///   This is the SERVER SIDE entry span. Combined with SoapHttpClient (the
    ///   caller-side exit span), both ends of every SOAP call are visible in Instana.
    ///
    /// TWO SPANS PER CALL (what Instana sees):
    ///   EXIT  span  — from SoapHttpClient  — "monolith called BookingUnit"
    ///   ENTRY span  — from InstanaSpan     — "BookingUnit was received and took Xms"
    /// </summary>
    public class OnlineBookingService : IOnlineBookingService
    {
        private readonly string _agentUrl;

        public OnlineBookingService()
            : this(Environment.GetEnvironmentVariable("INSTANA_AGENT_URL")
                   ?? "http://localhost:42699") { }

        /// <summary>Injectable constructor for tests — pass null agentUrl to disable tracing.</summary>
        public OnlineBookingService(string agentUrl)
        {
            _agentUrl = agentUrl;
        }

        // ── Diagramatic ───────────────────────────────────────────────────────

        public string retrieveComponentDiagramatic(string JSON)
        {
            // mirrors: using (var span = CustomSpan.Create()) { span.SetTag("soapAction",...); span.WrapAction(...); }
            using (var span = InstanaSpan.Create("retrieveComponentDiagramatic", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveComponentDiagramatic");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveComponentDiagramatic JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"data\":[{\"componentCode\":\"COMP-01\",\"componentName\":\"Tower A\",\"projectCode\":\"LP-JKT\"}]}";
                });
            }
        }

        public string retrieveComponentDiagramaticDetail(string JSON)
        {
            using (var span = InstanaSpan.Create("retrieveComponentDiagramaticDetail", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveComponentDiagramaticDetail");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveComponentDiagramaticDetail JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"data\":[{\"unitCode\":\"U-001\",\"floor\":5,\"available\":true}]}";
                });
            }
        }

        public string retrieveFloorDiagramatic(string JSON)
        {
            using (var span = InstanaSpan.Create("retrieveFloorDiagramatic", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveFloorDiagramatic");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveFloorDiagramatic JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"floors\":[{\"floorNo\":1},{\"floorNo\":2},{\"floorNo\":3}]}";
                });
            }
        }

        // ── Unit & booking ────────────────────────────────────────────────────

        public string retrieveAvailableUnitForOnlineBooking(string JSON)
        {
            using (var span = InstanaSpan.Create("retrieveAvailableUnitForOnlineBooking", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveAvailableUnitForOnlineBooking");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveAvailableUnitForOnlineBooking JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"units\":[{\"unitCode\":\"U-2BR-012\",\"type\":\"2BR\",\"price\":850000000}]}";
                });
            }
        }

        public string ReserveSelectedUnit(string JSON)
        {
            using (var span = InstanaSpan.Create("ReserveSelectedUnit", _agentUrl))
            {
                span.SetTag("soap.action", "ReserveSelectedUnit");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] ReserveSelectedUnit JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"reservationCode\":\"RES-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() + "\"}";
                });
            }
        }

        public string ReleaseSelectedUnit(string JSON)
        {
            using (var span = InstanaSpan.Create("ReleaseSelectedUnit", _agentUrl))
            {
                span.SetTag("soap.action", "ReleaseSelectedUnit");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] ReleaseSelectedUnit JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"message\":\"Unit released.\"}";
                });
            }
        }

        public string BookingUnit(string JSON)
        {
            // THE KEY OPERATION — matches the real LippoLand screenshot exactly:
            //   using (var span = CustomSpan.Create())
            //   {
            //       span.SetTag("soapAction", "BookingUnit");
            //       span.WrapAction(() => { ... }, true);
            //   }
            using (var span = InstanaSpan.Create("BookingUnit", _agentUrl))
            {
                span.SetTag("soap.action", "BookingUnit");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] BookingUnit JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"bookCode\":\"BK-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() + "\"}";
                });
            }
        }

        public string VerifyPayment(string JSON)
        {
            using (var span = InstanaSpan.Create("VerifyPayment", _agentUrl))
            {
                span.SetTag("soap.action", "VerifyPayment");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] VerifyPayment JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"verified\":true}";
                });
            }
        }

        // ── Pricing ───────────────────────────────────────────────────────────

        public string CalculateSellingPriceUnit(string JSON)
        {
            using (var span = InstanaSpan.Create("CalculateSellingPriceUnit", _agentUrl))
            {
                span.SetTag("soap.action", "CalculateSellingPriceUnit");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] CalculateSellingPriceUnit JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"sellingPrice\":850000000,\"tax\":85000000,\"total\":935000000}";
                });
            }
        }

        public string retrievePriceList(string JSON)
        {
            using (var span = InstanaSpan.Create("retrievePriceList", _agentUrl))
            {
                span.SetTag("soap.action", "retrievePriceList");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrievePriceList JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"pricelist\":[{\"unitType\":\"2BR\",\"basePrice\":850000000}]}";
                });
            }
        }

        // ── Customer ──────────────────────────────────────────────────────────

        public string retrieveCustomerProfile(string pscode)
        {
            using (var span = InstanaSpan.Create("retrieveCustomerProfile", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveCustomerProfile");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveCustomerProfile pscode={0}", pscode);
                    return "{\"status\":\"OK\",\"name\":\"Budi Santoso\",\"pscode\":\"" + pscode + "\"}";
                });
            }
        }

        public string doRegistrationCustomer(string JSON_IN)
        {
            using (var span = InstanaSpan.Create("doRegistrationCustomer", _agentUrl))
            {
                span.SetTag("soap.action", "doRegistrationCustomer");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] doRegistrationCustomer JSON={0}", JSON_IN);
                    return "{\"status\":\"OK\",\"pscode\":\"PS-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper() + "\"}";
                });
            }
        }

        // ── Booking history ───────────────────────────────────────────────────

        public string retrieveBookingHistoryCustomer(string pscode)
        {
            using (var span = InstanaSpan.Create("retrieveBookingHistoryCustomer", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveBookingHistoryCustomer");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveBookingHistoryCustomer pscode={0}", pscode);
                    return "{\"status\":\"OK\",\"bookings\":[{\"bookCode\":\"BK-DEMO-001\",\"status\":\"CONFIRMED\"}]}";
                });
            }
        }

        public string retrieveBookingDetail(string JSON)
        {
            using (var span = InstanaSpan.Create("retrieveBookingDetail", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveBookingDetail");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveBookingDetail JSON={0}", JSON);
                    return "{\"status\":\"OK\",\"bookCode\":\"BK-DEMO-001\",\"unitCode\":\"U-2BR-012\",\"status\":\"CONFIRMED\"}";
                });
            }
        }

        // ── Projects ──────────────────────────────────────────────────────────

        public string retrieveOnlineBookingProject()
        {
            using (var span = InstanaSpan.Create("retrieveOnlineBookingProject", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveOnlineBookingProject");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveOnlineBookingProject");
                    return "{\"status\":\"OK\",\"projects\":[{\"projectCode\":\"LP-JKT\",\"projectName\":\"Lippo Village Jakarta\"}]}";
                });
            }
        }

        public string retrieveOnlineBookingAllProject()
        {
            using (var span = InstanaSpan.Create("retrieveOnlineBookingAllProject", _agentUrl))
            {
                span.SetTag("soap.action", "retrieveOnlineBookingAllProject");
                return span.WrapAction(() =>
                {
                    Console.WriteLine("[Service] retrieveOnlineBookingAllProject");
                    return "{\"status\":\"OK\",\"projects\":[{\"projectCode\":\"LP-JKT\"},{\"projectCode\":\"LP-SBY\"}]}";
                });
            }
        }
    }
}
