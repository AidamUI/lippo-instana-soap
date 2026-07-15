using System;
using System.Xml;
using LippoLand.Soap.Client;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Unit tests for SoapEnvelopes.
    ///
    /// Covers:
    ///   1.  Well-formed XML output (parseable by XmlDocument)
    ///   2.  Correct SOAP 1.1 structure (Envelope / Header / Body)
    ///   3.  AuthHeader presence and correct element names
    ///   4.  Correct namespace (http://tempuri.org/)
    ///   5.  Correct SOAPAction operation element name
    ///   6.  JSON payload is round-tripped inside the body element
    ///   7.  XML special characters are escaped in the JSON payload
    ///   8.  XML special characters are escaped in AuthHeader fields
    ///   9.  Null JSON is handled (no NullReferenceException)
    ///   10. Null auth fields are handled
    ///   11. All six envelope methods produce non-empty output
    /// </summary>
    public static class SoapEnvelopesTests
    {
        private const string AUTH_DOMAIN = "LIPPOLAND";
        private const string AUTH_USER   = "testuser";
        private const string AUTH_PASS   = "testpass";

        public static void Run()
        {
            Test_WellFormedXml_retrieveComponentDiagramatic();
            Test_SoapEnvelope_HasCorrectNamespace();
            Test_SoapEnvelope_HasHeader_WithAuthHeader();
            Test_SoapEnvelope_AuthHeader_DomainUserPass();
            Test_SoapEnvelope_Body_HasOperationElement();
            Test_JsonPayload_IsPresent_InBody();
            Test_XmlSpecialChars_Escaped_InPayload();
            Test_XmlSpecialChars_Escaped_InAuthHeader();
            Test_NullJson_DoesNotThrow();
            Test_NullAuthFields_DoesNotThrow();
            Test_AllMethods_ReturnNonEmpty();
            Test_retrieveCustomerProfile_UsesPscodeParam();
            Console.WriteLine("  [SoapEnvelopesTests] All passed.");
        }

        static void Test_WellFormedXml_retrieveComponentDiagramatic()
        {
            string xml = SoapEnvelopes.retrieveComponentDiagramatic("{\"k\":\"v\"}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            var doc = new XmlDocument();
            doc.LoadXml(xml); // throws if malformed
            Assert.IsNotNull(doc.DocumentElement, "Root element missing");
        }

        static void Test_SoapEnvelope_HasCorrectNamespace()
        {
            string xml = SoapEnvelopes.BookingUnit("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.Contains(xml, "xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"");
            Assert.Contains(xml, "xmlns:tns=\"http://tempuri.org/\"");
        }

        static void Test_SoapEnvelope_HasHeader_WithAuthHeader()
        {
            string xml = SoapEnvelopes.ReserveSelectedUnit("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.Contains(xml, "<soap:Header>");
            Assert.Contains(xml, "<tns:AuthHeader>");
            Assert.Contains(xml, "</tns:AuthHeader>");
            Assert.Contains(xml, "</soap:Header>");
        }

        static void Test_SoapEnvelope_AuthHeader_DomainUserPass()
        {
            string xml = SoapEnvelopes.retrieveComponentDiagramatic("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.Contains(xml, "<tns:domainName>LIPPOLAND</tns:domainName>");
            Assert.Contains(xml, "<tns:userName>testuser</tns:userName>");
            Assert.Contains(xml, "<tns:password>testpass</tns:password>");
        }

        static void Test_SoapEnvelope_Body_HasOperationElement()
        {
            string xml = SoapEnvelopes.BookingUnit("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.Contains(xml, "<tns:BookingUnit>");
            Assert.Contains(xml, "</tns:BookingUnit>");
        }

        static void Test_JsonPayload_IsPresent_InBody()
        {
            string json = "{\"projectCode\":\"LP-JKT\",\"clusterCode\":\"CL-01\"}";
            string xml  = SoapEnvelopes.retrieveComponentDiagramatic(json, AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            // JSON is XML-escaped, so " becomes &quot; — check for the escaped form
            Assert.Contains(xml, "&quot;projectCode&quot;");
            Assert.Contains(xml, "LP-JKT");
        }

        static void Test_XmlSpecialChars_Escaped_InPayload()
        {
            // A JSON value containing < > & " ' — all must be escaped
            string json = "<script>&\"'";
            string xml  = SoapEnvelopes.retrieveComponentDiagramatic(json, AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.Contains(xml, "&lt;script&gt;");
            Assert.Contains(xml, "&amp;");
            Assert.Contains(xml, "&quot;");
            // Raw unescaped chars must NOT appear inside element content
            Assert.DoesNotContain(xml.Replace("<?xml", "").Replace("</", "").Replace("<tns:", "").Replace("<soap:", ""), "<script>");
        }

        static void Test_XmlSpecialChars_Escaped_InAuthHeader()
        {
            string xml = SoapEnvelopes.BookingUnit("{}", "DOM&AIN", "user<name>", "p@ss\"word");
            Assert.Contains(xml, "DOM&amp;AIN");
            Assert.Contains(xml, "user&lt;name&gt;");
            Assert.Contains(xml, "p@ss&quot;word");
        }

        static void Test_NullJson_DoesNotThrow()
        {
            // Should not throw — null becomes empty string
            string xml = SoapEnvelopes.retrieveComponentDiagramatic(null, AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.IsNotNullOrEmpty(xml);
        }

        static void Test_NullAuthFields_DoesNotThrow()
        {
            string xml = SoapEnvelopes.BookingUnit("{}", null, null, null);
            Assert.IsNotNullOrEmpty(xml);
            // Auth elements should be empty but present
            Assert.Contains(xml, "<tns:domainName></tns:domainName>");
        }

        static void Test_AllMethods_ReturnNonEmpty()
        {
            string[] envelopes = new[]
            {
                SoapEnvelopes.retrieveComponentDiagramatic("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.retrieveComponentDiagramaticDetail("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.retrieveFloorDiagramatic("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.retrieveAvailableUnitForOnlineBooking("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.ReserveSelectedUnit("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.BookingUnit("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.CalculateSellingPriceUnit("{}", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
                SoapEnvelopes.retrieveCustomerProfile("PS-001", AUTH_DOMAIN, AUTH_USER, AUTH_PASS),
            };
            foreach (string env in envelopes)
                Assert.IsNotNullOrEmpty(env, "An envelope method returned null/empty");
        }

        static void Test_retrieveCustomerProfile_UsesPscodeParam()
        {
            // retrieveCustomerProfile uses <tns:pscode> not <tns:JSON>
            string xml = SoapEnvelopes.retrieveCustomerProfile("PS-XYZ-99", AUTH_DOMAIN, AUTH_USER, AUTH_PASS);
            Assert.Contains(xml, "<tns:pscode>PS-XYZ-99</tns:pscode>");
            Assert.DoesNotContain(xml, "<tns:JSON>");
        }
    }
}
