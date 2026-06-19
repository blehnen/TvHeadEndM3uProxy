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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TvHeadEndM3uProxyService.Config;

namespace TvHeadEndM3uProxy
{
    /// <summary>
    /// Minimal-API endpoint filter that enforces an optional API key.
    /// When <see cref="ProxyOptions.ApiKey"/> is null or whitespace the endpoint is open.
    /// When set, the key must be supplied via query string <c>?apikey=</c> or header
    /// <c>X-Api-Key</c>. Uses a constant-time comparison to guard against timing attacks.
    /// The key value is never logged.
    /// </summary>
    public sealed class ApiKeyEndpointFilter : IEndpointFilter
    {
        private readonly ProxyOptions _options;
        private readonly ILogger<ApiKeyEndpointFilter> _logger;

        public ApiKeyEndpointFilter(IOptions<ProxyOptions> options, ILogger<ApiKeyEndpointFilter> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var expected = _options.ApiKey;

            // Open endpoint when no key is configured.
            if (string.IsNullOrWhiteSpace(expected))
            {
                return await next(context);
            }

            // Accept the key from query string first, then header.
            var request = context.HttpContext.Request;
            string? candidate = request.Query["apikey"];
            if (string.IsNullOrEmpty(candidate))
            {
                candidate = request.Headers["X-Api-Key"];
            }

            // Constant-time comparison that does NOT leak the configured key's length.
            // FixedTimeEquals requires equal-length spans; rather than branch on length
            // (which would leak the key length via timing — CWE-208), hash both inputs to
            // a fixed 32-byte SHA-256 digest and compare those. The compared length is
            // therefore always constant regardless of the supplied candidate's length.
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
            var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate ?? string.Empty));

            if (!CryptographicOperations.FixedTimeEquals(candidateHash, expectedHash))
            {
                // Generic message — do not reveal whether the length or the value differed,
                // and never log the key itself.
                _logger.LogWarning("API key check failed.");
                return Results.Unauthorized();
            }

            return await next(context);
        }
    }
}
