using CoreWCF;

namespace LippoLand.Soap.Service
{
    /// <summary>
    /// Service contract generated from the real WSDL at:
    ///   https://connect.lippoland.id/InternalMobileAppsService/WS_OnlineBooking.asmx?wsdl
    ///
    /// Key facts from the live WSDL:
    ///   - targetNamespace = http://tempuri.org/   (NOT a custom LippoLand namespace)
    ///   - Every operation accepts a single string parameter (JSON payload)
    ///   - Every operation returns a single string result (JSON response)
    ///   - A SOAP header AuthHeader (domainName, userName, password) is required
    ///
    /// We implement a representative subset of the real operations.
    /// CoreWCF replaces System.ServiceModel for Linux / .NET 8 compatibility.
    /// </summary>
    [ServiceContract(Namespace = "http://tempuri.org/",
                     Name      = "WS_OnlineBookingSoap")]
    public interface IOnlineBookingService
    {
        // ── Diagramatic (the operation your friend was working on) ────────────
        [OperationContract(Action = "http://tempuri.org/retrieveComponentDiagramatic")]
        string retrieveComponentDiagramatic(string JSON);

        [OperationContract(Action = "http://tempuri.org/retrieveComponentDiagramaticDetail")]
        string retrieveComponentDiagramaticDetail(string JSON);

        [OperationContract(Action = "http://tempuri.org/retrieveFloorDiagramatic")]
        string retrieveFloorDiagramatic(string JSON);

        // ── Unit availability & booking ───────────────────────────────────────
        [OperationContract(Action = "http://tempuri.org/retrieveAvailableUnitForOnlineBooking")]
        string retrieveAvailableUnitForOnlineBooking(string JSON);

        [OperationContract(Action = "http://tempuri.org/ReserveSelectedUnit")]
        string ReserveSelectedUnit(string JSON);

        [OperationContract(Action = "http://tempuri.org/ReleaseSelectedUnit")]
        string ReleaseSelectedUnit(string JSON);

        [OperationContract(Action = "http://tempuri.org/BookingUnit")]
        string BookingUnit(string JSON);

        [OperationContract(Action = "http://tempuri.org/VerifyPayment")]
        string VerifyPayment(string JSON);

        // ── Pricing ───────────────────────────────────────────────────────────
        [OperationContract(Action = "http://tempuri.org/CalculateSellingPriceUnit")]
        string CalculateSellingPriceUnit(string JSON);

        [OperationContract(Action = "http://tempuri.org/retrievePriceList")]
        string retrievePriceList(string JSON);

        // ── Customer ──────────────────────────────────────────────────────────
        [OperationContract(Action = "http://tempuri.org/retrieveCustomerProfile")]
        string retrieveCustomerProfile(string pscode);

        [OperationContract(Action = "http://tempuri.org/doRegistrationCustomer")]
        string doRegistrationCustomer(string JSON_IN);

        // ── Booking history ───────────────────────────────────────────────────
        [OperationContract(Action = "http://tempuri.org/retrieveBookingHistoryCustomer")]
        string retrieveBookingHistoryCustomer(string pscode);

        [OperationContract(Action = "http://tempuri.org/retrieveBookingDetail")]
        string retrieveBookingDetail(string JSON);

        // ── Projects ──────────────────────────────────────────────────────────
        [OperationContract(Action = "http://tempuri.org/retrieveOnlineBookingProject")]
        string retrieveOnlineBookingProject();

        [OperationContract(Action = "http://tempuri.org/retrieveOnlineBookingAllProject")]
        string retrieveOnlineBookingAllProject();
    }
}
