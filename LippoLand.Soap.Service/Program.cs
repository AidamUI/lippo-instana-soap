using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace LippoLand.Soap.Service
{
    /// <summary>
    /// Self-hosted WCF service host for .NET 4.0.
    /// Run this first, then run LippoLand.Soap.Client.
    ///
    /// WSDL is accessible at: http://localhost:8080/OnlineBooking?wsdl
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Uri baseAddress = new Uri("http://localhost:8080/OnlineBooking");

            using (ServiceHost host = new ServiceHost(typeof(OnlineBookingService), baseAddress))
            {
                // BasicHttpBinding = plain SOAP 1.1 over HTTP — same binding as the real ASMX
                BasicHttpBinding binding = new BasicHttpBinding
                {
                    MaxReceivedMessageSize = 10 * 1024 * 1024
                };

                host.AddServiceEndpoint(typeof(IOnlineBookingService), binding, "");

                // Enable WSDL / MEX — namespace is http://tempuri.org/ (matches real service)
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = true,
                    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 }
                };
                host.Description.Behaviors.Add(smb);

                // Add MEX endpoint (optional but conventional)
                host.AddServiceEndpoint(
                    ServiceMetadataBehavior.MexContractName,
                    MetadataExchangeBindings.CreateMexHttpBinding(),
                    "mex");

                host.Open();

                Console.WriteLine("=================================================");
                Console.WriteLine(" LippoLand SOAP Service is running.");
                Console.WriteLine(" Endpoint : {0}", baseAddress);
                Console.WriteLine(" WSDL     : {0}?wsdl", baseAddress);
                Console.WriteLine("=================================================");
                Console.WriteLine("Press ENTER to stop...");
                Console.ReadLine();

                host.Close();
            }
        }
    }
}
