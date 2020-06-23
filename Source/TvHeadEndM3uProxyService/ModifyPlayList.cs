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
using System.IO;
using PlaylistsNET.Content;
using TvHeadEndM3uProxyService.Config;

namespace TvHeadEndM3uProxyService
{
    internal class ModifyPlayList
    {
        public string Modify(Configuration config, string inputFile)
        {
            var content = new IPTVContent();
            using (var stream = new FileStream(inputFile, FileMode.Open))
            {
                var playlist = content.GetFromStream(stream);

                foreach (var item in playlist.PlaylistEntries)
                {
                    var path = item.Path;
                    var uri = new Uri(path);
                    var profile = string.Empty;
                    if (uri.PathAndQuery.Contains("&profile"))
                    {
                        profile = uri.PathAndQuery.Substring(uri.PathAndQuery.IndexOf("&profile", StringComparison.Ordinal));
                    }
                    var newPath =
                        $"{uri.Scheme}://{config.TvHeadEndUserName}:{config.TvHeadEndPassword}@{uri.Authority}{uri.AbsolutePath}{profile}";
                    item.Path = newPath;
                }

                var content2 = new IPTVContent();
                return content2.ToText(playlist);
            }
        }
    }
}
