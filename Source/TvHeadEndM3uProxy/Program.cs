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
using Serilog;
using SimpleInjector;
using Topshelf;
using Topshelf.SimpleInjector;
using TvHeadEndM3uProxyService;

namespace TvHeadEndM3uProxy
{
    class Program
    {
        private static readonly Container Container = new Container();

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            Log.Logger.Information("Startup");

            RegisterServices.Register(Container);

            HostFactory.Run(x =>
            {
                x.UseSimpleInjector(Container);

                x.Service<MainService>(s =>
                {
                    s.ConstructUsingSimpleInjector();
                    s.WhenStarted(p => p.Start());
                    s.WhenStopped(p => p.Stop());
                });
                x.RunAsNetworkService();

                x.StartAutomatically();
                x.UseSerilog(Log.Logger);
                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(0);
                    r.RestartService(1);
                    r.RestartService(2);
                    r.OnCrashOnly();
                    r.SetResetPeriod(1);
                });

                x.SetDescription("A proxy for the TvHeadEnd M3u files");
                x.SetDisplayName("TvHeadEndM3uProxy");
                x.SetServiceName("TvHeadEndM3uProxy");
            });

            Container.Dispose();
            Log.CloseAndFlush();
        }
    }
}
