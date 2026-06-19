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
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TvHeadEndM3uProxyService;
using TvHeadEndM3uProxyService.Config;
using TvHeadEndM3uProxyService.Tests.TestSupport;

namespace TvHeadEndM3uProxyService.Tests
{
    [TestClass]
    public class TvHeadendClientTests
    {
        private const string TestAddress = "http://tvheadend.local:9981";
        private const string TestUsername = "user";
        private const string TestPassword = "pass";
        private const string CannedM3u = "#EXTM3U\n#EXTINF:-1,Channel 1\nhttp://example.com/stream1\n";

        private static IOptions<TvHeadendOptions> BuildOptions(
            string address = TestAddress,
            string username = TestUsername,
            string password = TestPassword)
        {
            return Options.Create(new TvHeadendOptions
            {
                Address = address,
                Username = username,
                Password = password
            });
        }

        [TestMethod]
        public async Task FetchAsync_RequestUri_IsChannelsM3uEndpoint()
        {
            var handler = new StubHttpMessageHandler(CannedM3u);
            using var httpClient = new HttpClient(handler);
            var client = new TvHeadendClient(httpClient, BuildOptions());

            await client.FetchAsync();

            Assert.IsNotNull(handler.CapturedRequest);
            Assert.AreEqual(
                $"{TestAddress}/playlist/channels.m3u",
                handler.CapturedRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task FetchAsync_RequestMethod_IsGet()
        {
            var handler = new StubHttpMessageHandler(CannedM3u);
            using var httpClient = new HttpClient(handler);
            var client = new TvHeadendClient(httpClient, BuildOptions());

            await client.FetchAsync();

            Assert.IsNotNull(handler.CapturedRequest);
            Assert.AreEqual(HttpMethod.Get, handler.CapturedRequest!.Method);
        }

        [TestMethod]
        public async Task FetchAsync_AuthorizationHeader_IsExactBasicBase64()
        {
            var handler = new StubHttpMessageHandler(CannedM3u);
            using var httpClient = new HttpClient(handler);
            var client = new TvHeadendClient(httpClient, BuildOptions());

            await client.FetchAsync();

            Assert.IsNotNull(handler.CapturedRequest);
            var auth = handler.CapturedRequest!.Headers.Authorization;
            Assert.IsNotNull(auth);
            Assert.AreEqual("Basic", auth!.Scheme);

            var expectedParameter = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{TestUsername}:{TestPassword}"));
            Assert.AreEqual(expectedParameter, auth.Parameter);
        }

        [TestMethod]
        public async Task FetchAsync_ReturnedBody_MatchesCannedResponse()
        {
            var handler = new StubHttpMessageHandler(CannedM3u);
            using var httpClient = new HttpClient(handler);
            var client = new TvHeadendClient(httpClient, BuildOptions());

            var result = await client.FetchAsync();

            Assert.AreEqual(CannedM3u, result);
        }

        [TestMethod]
        public async Task FetchAsync_TrailingSlashInAddress_DoesNotDoubleSlash()
        {
            var handler = new StubHttpMessageHandler(CannedM3u);
            using var httpClient = new HttpClient(handler);
            var client = new TvHeadendClient(httpClient, BuildOptions(address: "http://tvheadend.local:9981/"));

            await client.FetchAsync();

            Assert.IsNotNull(handler.CapturedRequest);
            Assert.AreEqual(
                "http://tvheadend.local:9981/playlist/channels.m3u",
                handler.CapturedRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task FetchAsync_NonSuccessStatusCode_ThrowsHttpRequestException()
        {
            var handler = new StubHttpMessageHandler(CannedM3u, statusCode: HttpStatusCode.Unauthorized);
            using var httpClient = new HttpClient(handler);
            var client = new TvHeadendClient(httpClient, BuildOptions());

            bool threw = false;
            try
            {
                await client.FetchAsync();
            }
            catch (HttpRequestException)
            {
                threw = true;
            }
            Assert.IsTrue(threw, "Expected HttpRequestException for non-success status code.");
        }
    }
}
