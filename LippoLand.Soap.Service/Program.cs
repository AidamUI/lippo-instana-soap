using System;
using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LippoLand.Soap.Service
{
    /// <summary>
    /// Self-hosted CoreWCF service — Linux / .NET 8 replacement for the
    /// classic WCF self-host. Exposes BasicHttpBinding on :8080.
    ///
    /// WSDL: http://localhost:8080/OnlineBooking?wsdl
    ///
    /// Run this first, then run LippoLand.Soap.Client.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://0.0.0.0:8080");

            builder.Services.AddServiceModelServices();
            builder.Services.AddServiceModelMetadata();
            builder.Services.AddSingleton<OnlineBookingService>();

            var app = builder.Build();

            app.UseServiceModel(svc =>
            {
                svc.AddService<OnlineBookingService>(opts =>
                {
                    // Base address — WSDL will be at /OnlineBooking?wsdl
                    opts.BaseAddresses.Add(new Uri("http://localhost:8080/OnlineBooking"));
                });

                // Empty relative path → endpoint lives exactly at the base address
                svc.AddServiceEndpoint<OnlineBookingService, IOnlineBookingService>(
                    new BasicHttpBinding
                    {
                        MaxReceivedMessageSize = 10 * 1024 * 1024
                    },
                    "");
            });

            var smb = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
            smb.HttpGetEnabled = true;

            Console.WriteLine("=================================================");
            Console.WriteLine(" LippoLand SOAP Service is running.");
            Console.WriteLine(" Endpoint : http://localhost:8080/OnlineBooking");
            Console.WriteLine(" WSDL     : http://localhost:8080/OnlineBooking?wsdl");
            Console.WriteLine("=================================================");

            app.Run();
        }
    }
}
