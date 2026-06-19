// ---------------------------------------------------------------------
// MIT License
//
// Copyright (c) 2020 Brian Lehnen
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TvHeadEndM3uProxyService;
using TvHeadEndM3uProxyService.Tests.TestSupport;

namespace TvHeadEndM3uProxyService.Tests
{
    // ---------------------------------------------------------------------------
    // Canned upstream M3U returned by the stub on every happy-path call.
    // Uses simple credentials (user/pass) so Uri.EscapeDataString is a no-op
    // and the expected output is byte-identical to the lf fixture.
    //
    // Input (what TvHeadend would return):
    //   #EXTM3U
    //   #EXTINF:-1,Channel One
    //   http://127.0.0.1:9981/stream/channelid/1234?ticket=ABC&profile=pass
    //
    // Expected rewrite (ticket dropped, creds injected, &profile retained):
    //   #EXTM3U
    //   #EXTINF:-1,Channel One
    //   http://user:pass@127.0.0.1:9981/stream/channelid/1234&profile=pass
    // ---------------------------------------------------------------------------
    [TestClass]
    public class ChannelsEndpointTests
    {
        private const string EndpointUrl = "/api/tvheadend/channels";

        // Canned upstream body (what TvHeadend returns to TvHeadendClient.FetchAsync).
        private const string UpstreamBody =
            "#EXTM3U\n" +
            "#EXTINF:-1,Channel One\n" +
            "http://127.0.0.1:9981/stream/channelid/1234?ticket=ABC&profile=pass\n";

        // The expected rewritten body after PlaylistRewriter runs with user/pass credentials.
        // Ticket is dropped; user:pass@ injected; &profile suffix retained.
        private const string ExpectedBody =
            "#EXTM3U\n" +
            "#EXTINF:-1,Channel One\n" +
            "http://user:pass@127.0.0.1:9981/stream/channelid/1234&profile=pass\n";

        private static readonly byte[] ExpectedBytes = Encoding.UTF8.GetBytes(ExpectedBody);

        // -----------------------------------------------------------------------
        // Factory helper — builds a WAF<Program> with required config injected
        // and the TvHeadendClient's primary HTTP handler replaced with the stub.
        //
        // Parameters:
        //   stub          — pre-configured StubHttpMessageHandler (caller owns lifetime)
        //   apiKey        — when non-null, sets Proxy:ApiKey (enables the key gate)
        //   cacheTtlSecs  — when > 0, sets Proxy:CacheTtlSeconds
        // -----------------------------------------------------------------------
        private static WebApplicationFactory<Program> BuildFactory(
            StubHttpMessageHandler stub,
            string? apiKey = null,
            int cacheTtlSecs = 0)
        {
            return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            {
                b.ConfigureAppConfiguration((_, cfg) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        // Required by ValidateOnStart on TvHeadendOptions.
                        ["TvHeadend:Address"]   = "http://127.0.0.1:9981",
                        ["TvHeadend:Username"]  = "user",
                        ["TvHeadend:Password"]  = "pass",

                        // Proxy options — always set both so defaults are explicit.
                        ["Proxy:ApiKey"]          = apiKey,
                        ["Proxy:CacheTtlSeconds"] = cacheTtlSecs.ToString(),
                    };
                    cfg.AddInMemoryCollection(settings);
                });

                b.ConfigureTestServices(services =>
                {
                    // Override the typed client's primary handler so no real network
                    // is hit. ConfigureTestServices runs after the production
                    // ConfigureServices, so this re-registration wins.
                    services
                        .AddHttpClient<TvHeadendClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => stub);
                });
            });
        }

        // -----------------------------------------------------------------------
        // Test 1: Happy path — no API key required.
        // Asserts: 200, body bytes == expected rewritten m3u,
        //          Content-Type == application/octet-stream,
        //          Content-Disposition == attachment; filename="channels.m3u".
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task HappyPath_Returns200WithRewrittenPlaylist()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub);
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(EndpointUrl);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            // Content-Type
            Assert.AreEqual(
                "application/octet-stream",
                response.Content.Headers.ContentType?.MediaType,
                "Content-Type must be application/octet-stream");

            // Content-Disposition
            var disposition = response.Content.Headers.ContentDisposition;
            Assert.IsNotNull(disposition, "Content-Disposition header must be present");
            Assert.AreEqual("attachment", disposition!.DispositionType,
                "Content-Disposition must be 'attachment'");
            Assert.AreEqual("channels.m3u", disposition.FileName,
                "Content-Disposition filename must be 'channels.m3u'");

            // Body bytes
            var actualBytes = await response.Content.ReadAsByteArrayAsync();
            CollectionAssert.AreEqual(ExpectedBytes, actualBytes,
                "Rewritten body bytes must match expected playlist");
        }

        // -----------------------------------------------------------------------
        // Test 2: API key OFF (unset / null) — request without key -> 200.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task ApiKeyOff_NoKeyInRequest_Returns200()
        {
            // apiKey = null -> ProxyOptions.ApiKey is null/empty -> endpoint is open.
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, apiKey: null);
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(EndpointUrl);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // -----------------------------------------------------------------------
        // Test 3: API key ON, no key in request -> 401.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task ApiKeyOn_NoKeyInRequest_Returns401()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, apiKey: "secret123");
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(EndpointUrl);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // -----------------------------------------------------------------------
        // Test 4: API key ON, wrong key in request -> 401.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task ApiKeyOn_WrongKeyInRequest_Returns401()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, apiKey: "secret123");
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(EndpointUrl + "?apikey=wrongkey");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // -----------------------------------------------------------------------
        // Test 5: API key ON, correct key via ?apikey= query string -> 200.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task ApiKeyOn_CorrectKeyViaQueryString_Returns200()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, apiKey: "secret123");
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(EndpointUrl + "?apikey=secret123");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // -----------------------------------------------------------------------
        // Test 6: API key ON, correct key via X-Api-Key header -> 200.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task ApiKeyOn_CorrectKeyViaHeader_Returns200()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, apiKey: "secret123");
            using var client = factory.CreateClient();

            using var request = new HttpRequestMessage(HttpMethod.Get, EndpointUrl);
            request.Headers.Add("X-Api-Key", "secret123");
            using var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // -----------------------------------------------------------------------
        // Test 7: Upstream failure — stub returns 500 -> endpoint returns 503.
        // TvHeadendClient calls EnsureSuccessStatusCode() which throws
        // HttpRequestException on non-2xx, which the endpoint catches -> 503.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task UpstreamFailure_Returns503()
        {
            var stub = new StubHttpMessageHandler(string.Empty, HttpStatusCode.InternalServerError);
            using var factory = BuildFactory(stub);
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(EndpointUrl);

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        // -----------------------------------------------------------------------
        // Test 8: Cache enabled — two requests with CacheTtlSeconds=60 must hit
        //         the stub only once (second request served from IMemoryCache).
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task Cache_Enabled_SecondRequestServedFromCache()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, cacheTtlSecs: 60);
            using var client = factory.CreateClient();

            using var response1 = await client.GetAsync(EndpointUrl);
            using var response2 = await client.GetAsync(EndpointUrl);

            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);

            // Only one upstream call should have been made.
            Assert.AreEqual(1, stub.CallCount,
                "With cache enabled, the stub should only be called once across two requests");

            // Both responses should carry the same body.
            var bytes1 = await response1.Content.ReadAsByteArrayAsync();
            var bytes2 = await response2.Content.ReadAsByteArrayAsync();
            CollectionAssert.AreEqual(bytes1, bytes2,
                "Both cached and uncached responses must have identical bodies");
        }

        // -----------------------------------------------------------------------
        // Test 9: Cache disabled (TTL=0) — two requests must hit the stub twice.
        // -----------------------------------------------------------------------
        [TestMethod]
        public async Task Cache_Disabled_EachRequestHitsUpstream()
        {
            var stub = new StubHttpMessageHandler(UpstreamBody);
            using var factory = BuildFactory(stub, cacheTtlSecs: 0);
            using var client = factory.CreateClient();

            using var response1 = await client.GetAsync(EndpointUrl);
            using var response2 = await client.GetAsync(EndpointUrl);

            Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);

            Assert.AreEqual(2, stub.CallCount,
                "With cache disabled, each request must call the upstream once");
        }
    }
}
