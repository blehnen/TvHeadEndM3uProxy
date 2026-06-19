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
using Microsoft.Extensions.Configuration;

namespace TvHeadEndM3uProxyService.Tests
{
    [TestClass]
    public class HealthCheckProbeTests
    {
        /// <summary>
        /// When no env-var overrides are supplied and config["Urls"] is the default
        /// appsettings value, the resolved port is 33721.
        /// </summary>
        [TestMethod]
        public void DefaultPort_ResolvedFrom_AppSettings()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Urls"] = "http://*:33721"
                })
                .Build();

            // Pass null for both env-var parameters to simulate their absence.
            var port = Program.ResolveHealthCheckPort(config, aspnetcoreUrls: null, dotnetUrls: null);

            Assert.AreEqual(33721, port);
        }

        /// <summary>
        /// When ASPNETCORE_URLS is supplied it must win over config["Urls"] and the default.
        /// </summary>
        [TestMethod]
        public void EnvVarOverride_AspnetcoreUrls_WinsOverConfig()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Urls"] = "http://*:33721"
                })
                .Build();

            // Simulate ASPNETCORE_URLS=http://*:9999 via the parameter override.
            var port = Program.ResolveHealthCheckPort(config, aspnetcoreUrls: "http://*:9999", dotnetUrls: null);

            Assert.AreEqual(9999, port);
        }
    }
}
