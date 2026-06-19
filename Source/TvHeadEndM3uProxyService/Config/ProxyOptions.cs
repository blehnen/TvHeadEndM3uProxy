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

namespace TvHeadEndM3uProxyService.Config
{
    public sealed class ProxyOptions
    {
        public const string SectionName = "Proxy";

        /// <summary>
        /// Optional API key. When null or empty the endpoint is open (no auth required).
        /// Maps from env var PROXY_API_KEY.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Cache TTL in seconds. 0 (default) disables the cache.
        /// Maps from env var CACHE_TTL_SECONDS.
        /// </summary>
        public int CacheTtlSeconds { get; set; }
    }
}
