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
using Swan.Logging;
namespace TvHeadEndM3uProxyService.Logging
{
    internal class WebServerLogger : ILogger
    {
        private readonly Serilog.ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public WebServerLogger(Serilog.ILogger logger)
        {
            _logger = logger;
            LogLevel = TranslateLogLevel(logger);
        }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        public LogLevel LogLevel { get; }

        /// <summary>
        /// Logs the specified log event.
        /// </summary>
        /// <param name="logEvent">The <see cref="T:Swan.LogMessageReceivedEventArgs" /> instance containing the event data.</param>
        public void Log(LogMessageReceivedEventArgs logEvent)
        {
            switch (logEvent.MessageType)
            {
                case LogLevel.None:
                    break;
                case LogLevel.Info:
                    _logger.Information("{@logEvent}", logEvent);
                    break;
                case LogLevel.Trace:
                    _logger.Verbose("{@logEvent}", logEvent);
                    break;
                case LogLevel.Debug:
                    _logger.Debug("{@logEvent}", logEvent);
                    break;
                case LogLevel.Warning:
                    _logger.Warning("{@logEvent}", logEvent);
                    break;
                case LogLevel.Error:
                    _logger.Error("{@logEvent}", logEvent);
                    break;
                case LogLevel.Fatal:
                    _logger.Fatal("{@logEvent}", logEvent);
                    break;
            }
        }

        /// <summary>
        /// Translates the log level from serilog to the web server log level
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        private LogLevel TranslateLogLevel(Serilog.ILogger logger)
        {
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                return LogLevel.Trace;
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                return LogLevel.Debug;
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Information))
                return LogLevel.Info;
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Warning))
                return LogLevel.Warning;
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Error))
                return LogLevel.Error;
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Fatal))
                return LogLevel.Fatal;

            return LogLevel.None;
        }

        #region IDisposable Support - required because iLogger has it
        private bool _disposedValue; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }
                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
