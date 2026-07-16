using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Fake HttpMessageHandler used to intercept calls to the Instana agent
    /// in unit tests. Records every request so tests can assert on:
    ///   - soap.action tag in the span payload
    ///   - soap.operation tag
    ///   - soap.endpoint tag
    ///   - X-INSTANA-T / X-INSTANA-S headers forwarded to the SOAP target
    /// </summary>
    public sealed class InstanaHandlerStub : HttpMessageHandler
    {
        public readonly List<(string Url, string Body)> Requests
            = new List<(string, string)>();

        protected override HttpResponseMessage Send(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content != null
                ? request.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                : "";
            Requests.Add((request.RequestUri.ToString(), body));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Send(request, cancellationToken));
        }
    }
}
