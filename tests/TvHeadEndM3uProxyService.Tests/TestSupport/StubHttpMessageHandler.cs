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
using System.Threading;
using System.Threading.Tasks;

namespace TvHeadEndM3uProxyService.Tests.TestSupport
{
    /// <summary>
    /// A reusable fake <see cref="HttpMessageHandler"/> for unit and integration tests.
    /// Captures the last incoming request and tracks call count; supports a configurable
    /// response body, status code, media type, and an optional factory delegate for
    /// returning custom responses or throwing exceptions.
    /// </summary>
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _statusCode;
        private readonly string _mediaType;

        /// <summary>
        /// The most recent <see cref="HttpRequestMessage"/> received by this handler.
        /// </summary>
        public HttpRequestMessage? CapturedRequest { get; private set; }

        /// <summary>
        /// The total number of times <see cref="SendAsync"/> has been invoked.
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// When set, this factory is invoked instead of building the default response.
        /// Use it to return a custom <see cref="HttpResponseMessage"/> or to throw an
        /// exception (e.g. to simulate a 503 / network failure scenario).
        /// </summary>
        public Func<HttpResponseMessage>? ResponseFactory { get; set; }

        /// <summary>
        /// Initializes a new <see cref="StubHttpMessageHandler"/>.
        /// </summary>
        /// <param name="body">The response body returned for every request.</param>
        /// <param name="statusCode">The HTTP status code (default 200 OK).</param>
        /// <param name="mediaType">The media type of the response content (default "audio/x-mpegurl").</param>
        public StubHttpMessageHandler(
            string body,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            string mediaType = "audio/x-mpegurl")
        {
            _body = body;
            _statusCode = statusCode;
            _mediaType = mediaType;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            CapturedRequest = request;

            var response = ResponseFactory != null
                ? ResponseFactory()
                : new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_body, Encoding.UTF8, _mediaType)
                };

            return Task.FromResult(response);
        }
    }
}
