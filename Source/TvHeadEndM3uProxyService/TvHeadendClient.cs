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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TvHeadEndM3uProxyService.Config;

namespace TvHeadEndM3uProxyService
{
    public sealed class TvHeadendClient
    {
        private readonly HttpClient _httpClient;
        private readonly TvHeadendOptions _options;

        public TvHeadendClient(HttpClient httpClient, IOptions<TvHeadendOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            // Apply a sane default timeout when DI configuration is not yet present;
            // Phase 3 (AddHttpClient) may override this at the handler level.
            if (_httpClient.Timeout == Timeout.InfiniteTimeSpan)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
            }
        }

        /// <summary>
        /// Fetches the raw M3U playlist from {Address}/playlist/channels.m3u in memory.
        /// Credentials are never logged.
        /// </summary>
        public async Task<string> FetchAsync(CancellationToken ct = default)
        {
            var uri = $"{_options.Address.TrimEnd('/')}/playlist/channels.m3u";
            var credential = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credential);

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
    }
}
