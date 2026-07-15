namespace LippoLand.Soap.Client
{
    /// <summary>
    /// SOAP 1.1 envelopes for WS_OnlineBooking operations.
    ///
    /// Facts from the real WSDL (https://connect.lippoland.id/...?wsdl):
    ///   - targetNamespace = http://tempuri.org/
    ///   - Every operation takes a single string param (JSON payload)
    ///   - A SOAP header AuthHeader (domainName, userName, password) is required
    ///
    /// The AuthHeader is injected by BuildEnvelope() so every operation gets it.
    /// </summary>
    public static class SoapEnvelopes
    {
        private const string NS = "http://tempuri.org/";

        private const string EnvelopeOpen =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
            " xmlns:tns=\"" + NS + "\">";

        private const string EnvelopeClose = "</soap:Envelope>";

        /// <summary>
        /// Wraps a body fragment with the full SOAP envelope including AuthHeader.
        /// </summary>
        private static string BuildEnvelope(string authDomain, string authUser, string authPass, string body)
        {
            string header = string.Format(
                "<soap:Header>" +
                "<tns:AuthHeader>" +
                "<tns:domainName>{0}</tns:domainName>" +
                "<tns:userName>{1}</tns:userName>" +
                "<tns:password>{2}</tns:password>" +
                "</tns:AuthHeader>" +
                "</soap:Header>",
                Encode(authDomain), Encode(authUser), Encode(authPass));

            return EnvelopeOpen + header + "<soap:Body>" + body + "</soap:Body>" + EnvelopeClose;
        }

        // ── Diagramatic operations (the ones your friend was working on) ──────

        public static string retrieveComponentDiagramatic(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:retrieveComponentDiagramatic><tns:JSON>{0}</tns:JSON></tns:retrieveComponentDiagramatic>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        public static string retrieveComponentDiagramaticDetail(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:retrieveComponentDiagramaticDetail><tns:JSON>{0}</tns:JSON></tns:retrieveComponentDiagramaticDetail>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        public static string retrieveFloorDiagramatic(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:retrieveFloorDiagramatic><tns:JSON>{0}</tns:JSON></tns:retrieveFloorDiagramatic>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        // ── Unit & booking ────────────────────────────────────────────────────

        public static string retrieveAvailableUnitForOnlineBooking(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:retrieveAvailableUnitForOnlineBooking><tns:JSON>{0}</tns:JSON></tns:retrieveAvailableUnitForOnlineBooking>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        public static string ReserveSelectedUnit(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:ReserveSelectedUnit><tns:JSON>{0}</tns:JSON></tns:ReserveSelectedUnit>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        public static string BookingUnit(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:BookingUnit><tns:JSON>{0}</tns:JSON></tns:BookingUnit>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        // ── Pricing ───────────────────────────────────────────────────────────

        public static string CalculateSellingPriceUnit(string json, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:CalculateSellingPriceUnit><tns:JSON>{0}</tns:JSON></tns:CalculateSellingPriceUnit>",
                Encode(json));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        // ── Customer ──────────────────────────────────────────────────────────

        public static string retrieveCustomerProfile(string pscode, string authDomain, string authUser, string authPass)
        {
            string body = string.Format(
                "<tns:retrieveCustomerProfile><tns:pscode>{0}</tns:pscode></tns:retrieveCustomerProfile>",
                Encode(pscode));
            return BuildEnvelope(authDomain, authUser, authPass, body);
        }

        private static string Encode(string value)
        {
            return System.Security.SecurityElement.Escape(value ?? string.Empty);
        }
    }
}
