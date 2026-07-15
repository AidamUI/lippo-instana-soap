using System;

namespace LippoLand.Soap.Service
{
    /// <summary>
    /// Stub implementation of the real WS_OnlineBooking contract.
    /// Returns JSON strings that mirror the real service's response shape
    /// so the client integration test works without hitting the live backend.
    /// </summary>
    public class OnlineBookingService : IOnlineBookingService
    {
        public string retrieveComponentDiagramatic(string JSON)
        {
            Console.WriteLine("[Service] retrieveComponentDiagramatic JSON={0}", JSON);
            return "{\"status\":\"OK\",\"data\":[{\"componentCode\":\"COMP-01\",\"componentName\":\"Tower A\",\"projectCode\":\"LP-JKT\"}]}";
        }

        public string retrieveComponentDiagramaticDetail(string JSON)
        {
            Console.WriteLine("[Service] retrieveComponentDiagramaticDetail JSON={0}", JSON);
            return "{\"status\":\"OK\",\"data\":[{\"unitCode\":\"U-001\",\"floor\":5,\"available\":true}]}";
        }

        public string retrieveFloorDiagramatic(string JSON)
        {
            Console.WriteLine("[Service] retrieveFloorDiagramatic JSON={0}", JSON);
            return "{\"status\":\"OK\",\"floors\":[{\"floorNo\":1},{\"floorNo\":2},{\"floorNo\":3}]}";
        }

        public string retrieveAvailableUnitForOnlineBooking(string JSON)
        {
            Console.WriteLine("[Service] retrieveAvailableUnitForOnlineBooking JSON={0}", JSON);
            return "{\"status\":\"OK\",\"units\":[{\"unitCode\":\"U-2BR-012\",\"type\":\"2BR\",\"price\":850000000}]}";
        }

        public string ReserveSelectedUnit(string JSON)
        {
            Console.WriteLine("[Service] ReserveSelectedUnit JSON={0}", JSON);
            return "{\"status\":\"OK\",\"reservationCode\":\"RES-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() + "\"}";
        }

        public string ReleaseSelectedUnit(string JSON)
        {
            Console.WriteLine("[Service] ReleaseSelectedUnit JSON={0}", JSON);
            return "{\"status\":\"OK\",\"message\":\"Unit released.\"}";
        }

        public string BookingUnit(string JSON)
        {
            Console.WriteLine("[Service] BookingUnit JSON={0}", JSON);
            return "{\"status\":\"OK\",\"bookCode\":\"BK-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() + "\"}";
        }

        public string VerifyPayment(string JSON)
        {
            Console.WriteLine("[Service] VerifyPayment JSON={0}", JSON);
            return "{\"status\":\"OK\",\"verified\":true}";
        }

        public string CalculateSellingPriceUnit(string JSON)
        {
            Console.WriteLine("[Service] CalculateSellingPriceUnit JSON={0}", JSON);
            return "{\"status\":\"OK\",\"sellingPrice\":850000000,\"tax\":85000000,\"total\":935000000}";
        }

        public string retrievePriceList(string JSON)
        {
            Console.WriteLine("[Service] retrievePriceList JSON={0}", JSON);
            return "{\"status\":\"OK\",\"pricelist\":[{\"unitType\":\"2BR\",\"basePrice\":850000000}]}";
        }

        public string retrieveCustomerProfile(string pscode)
        {
            Console.WriteLine("[Service] retrieveCustomerProfile pscode={0}", pscode);
            return "{\"status\":\"OK\",\"name\":\"Budi Santoso\",\"pscode\":\"" + pscode + "\"}";
        }

        public string doRegistrationCustomer(string JSON_IN)
        {
            Console.WriteLine("[Service] doRegistrationCustomer JSON={0}", JSON_IN);
            return "{\"status\":\"OK\",\"pscode\":\"PS-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper() + "\"}";
        }

        public string retrieveBookingHistoryCustomer(string pscode)
        {
            Console.WriteLine("[Service] retrieveBookingHistoryCustomer pscode={0}", pscode);
            return "{\"status\":\"OK\",\"bookings\":[{\"bookCode\":\"BK-DEMO-001\",\"status\":\"CONFIRMED\"}]}";
        }

        public string retrieveBookingDetail(string JSON)
        {
            Console.WriteLine("[Service] retrieveBookingDetail JSON={0}", JSON);
            return "{\"status\":\"OK\",\"bookCode\":\"BK-DEMO-001\",\"unitCode\":\"U-2BR-012\",\"status\":\"CONFIRMED\"}";
        }

        public string retrieveOnlineBookingProject()
        {
            Console.WriteLine("[Service] retrieveOnlineBookingProject");
            return "{\"status\":\"OK\",\"projects\":[{\"projectCode\":\"LP-JKT\",\"projectName\":\"Lippo Village Jakarta\"}]}";
        }

        public string retrieveOnlineBookingAllProject()
        {
            Console.WriteLine("[Service] retrieveOnlineBookingAllProject");
            return "{\"status\":\"OK\",\"projects\":[{\"projectCode\":\"LP-JKT\"},{\"projectCode\":\"LP-SBY\"}]}";
        }
    }
}
