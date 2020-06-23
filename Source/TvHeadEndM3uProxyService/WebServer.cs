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
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebApi;
using Serilog;
using Swan.Logging;
using TvHeadEndM3uProxyService.Config;
using TvHeadEndM3uProxyService.Controllers;
using TvHeadEndM3uProxyService.Logging;

namespace TvHeadEndM3uProxyService
{
    /// <summary>
    /// The entry-point service
    /// </summary>
    public class WebServer
    {
        private CancellationTokenSource _cancelToken;
        private Task _webServerTask;

        private IWebServer _server;
        private readonly object _locker = new object();

        private readonly Configuration _config;

        private readonly TvHeadendController _controller;

        public WebServer(Configuration config, TvHeadendController controller)
        {
            _config = config;
            _controller = controller;
        }
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            try
            {
                lock (_locker) //prevent stop being called before start finishes
                {
                    _cancelToken = new CancellationTokenSource();

                    Log.Logger.Information($"Starting web server at [{_config.HostAddress}]...");
                    _server = CreateWebServer(new[] { _config.HostAddress});
                    _webServerTask = _server.RunAsync(_cancelToken.Token);
                    Log.Logger.Information("Web server started");
                }
            }
            catch(Exception error)
            {
                Log.Logger.Fatal($"A fatal error has occured while starting {error}");
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            try
            {
                lock (_locker) //prevent stop being called before start finishes
                {
                    _cancelToken?.Cancel(); //request that the web server stop
                    _webServerTask?.Wait();  //queue has shutdown - now shutdown the web server

                    //server must be disposed to release the TCP port(s)
                    _server?.Dispose();
                    _cancelToken?.Dispose();
                }
            }
            catch (Exception error)
            {
                Log.Logger.Fatal(error, $"A fatal error has occured while stopping {error}");
            }
        }

        /// <summary>
        /// Creates the web server.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <returns></returns>
        private IWebServer CreateWebServer(string[] urls)
        {
            //remove built in console logger and register the override that wraps serilog
#if DEBUG
            Logger.UnregisterLogger<ConsoleLogger>();
#endif
            Logger.RegisterLogger(new WebServerLogger(Log.Logger));

            WebServerOptions options = new WebServerOptions();
            options.WithUrlPrefixes(urls);
            options.WithMode(HttpListenerMode.Microsoft);
            var server = new EmbedIO.WebServer(options);

            server.WithWebApi("/api", m => m
                .WithController(() => _controller));

            return server;
        }
    }
}
