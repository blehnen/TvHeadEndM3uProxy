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
using System.Text;
using TvHeadEndM3uProxyService.Config;

namespace TvHeadEndM3uProxyService
{
    /// <summary>
    /// Pure, in-house M3U rewrite. Only stream-URL lines are transformed; all other
    /// lines (#EXTM3U, #EXTINF, comments, blanks) and original line endings are
    /// preserved byte-for-byte (verbatim passthrough contract).
    /// </summary>
    public sealed class PlaylistRewriter
    {
        /// <summary>
        /// Rewrites <paramref name="playlist"/> by injecting credentials into every
        /// http/https stream-URL line and dropping the expiring ticket query parameter.
        /// </summary>
        /// <param name="playlist">Raw M3U text received from TvHeadend.</param>
        /// <param name="username">Username to inject into the URL authority.</param>
        /// <param name="password">Password to inject into the URL authority.</param>
        /// <returns>Rewritten M3U text with original line endings preserved.</returns>
        public string Rewrite(string playlist, string username, string password)
        {
            if (string.IsNullOrEmpty(playlist))
            {
                return playlist;
            }

            var result = new StringBuilder(playlist.Length);
            var pos = 0;

            while (pos < playlist.Length)
            {
                // Locate end of current line, capturing the line ending verbatim.
                var lineStart = pos;
                var lineEnd = pos;
                while (lineEnd < playlist.Length && playlist[lineEnd] != '\n')
                {
                    lineEnd++;
                }

                // lineEnd now points at '\n' or end-of-string.
                string lineEnding;
                int contentEnd;
                if (lineEnd < playlist.Length)
                {
                    // Include the '\n'.
                    lineEnding = "\n";
                    // Check for preceding '\r' (CRLF).
                    if (lineEnd > lineStart && playlist[lineEnd - 1] == '\r')
                    {
                        lineEnding = "\r\n";
                        contentEnd = lineEnd - 1;
                    }
                    else
                    {
                        contentEnd = lineEnd;
                    }
                    pos = lineEnd + 1;
                }
                else
                {
                    // Last segment with no trailing newline.
                    lineEnding = string.Empty;
                    contentEnd = lineEnd;
                    pos = lineEnd;
                }

                var lineContent = playlist.Substring(lineStart, contentEnd - lineStart);

                result.Append(TransformLine(lineContent, username, password));
                result.Append(lineEnding);
            }

            return result.ToString();
        }

        /// <summary>
        /// Convenience overload that reads credentials from <see cref="TvHeadendOptions"/>.
        /// </summary>
        public string Rewrite(string playlist, TvHeadendOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return Rewrite(playlist, options.Username, options.Password);
        }

        // Applies the URL transform if the line is an http/https stream URL; otherwise
        // returns the line unchanged.
        private static string TransformLine(string line, string username, string password)
        {
            if (!Uri.TryCreate(line, UriKind.Absolute, out var uri))
            {
                return line;
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return line;
            }

            // Build the rewritten query: drop the expiring "ticket" parameter and keep
            // every other parameter (profile, etc.) in its original order, with a
            // spec-correct leading '?'. PathAndQuery is used (not Uri.Query) because
            // Uri.Query normalises %2B etc; the kept parameters are plain ASCII.
            var querySuffix = BuildQuerySuffix(uri.PathAndQuery);

            // String-compose the rewritten URL (never UriBuilder — it re-encodes the path).
            // Encode each credential component so special chars (@, :, /, space, etc.)
            // produce a valid URL authority (Uri.EscapeDataString is a no-op for plain
            // ASCII alnum values such as "user"/"pass").
            var u = Uri.EscapeDataString(username);
            var p = Uri.EscapeDataString(password);
            return $"{uri.Scheme}://{u}:{p}@{uri.Authority}{uri.AbsolutePath}{querySuffix}";
        }

        // Rebuilds the query string after dropping the "ticket" parameter, preserving
        // the order and raw bytes of every other parameter. Returns a spec-correct
        // suffix ("?a=1&b=2") or empty string when nothing remains. Handles a profile
        // (or any param) appearing first — it becomes the leading '?a=...' rather than
        // being lost or glued on with a stray '&'.
        private static string BuildQuerySuffix(string pathAndQuery)
        {
            var q = pathAndQuery.IndexOf('?');
            if (q < 0)
            {
                return string.Empty;
            }

            var query = pathAndQuery.Substring(q + 1);
            if (query.Length == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            foreach (var part in query.Split('&'))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                var eq = part.IndexOf('=');
                var key = eq < 0 ? part : part.Substring(0, eq);
                if (string.Equals(key, "ticket", StringComparison.Ordinal))
                {
                    continue;
                }

                result.Append(result.Length == 0 ? '?' : '&').Append(part);
            }

            return result.ToString();
        }
    }
}
