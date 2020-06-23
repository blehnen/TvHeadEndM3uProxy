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
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Serilog;

namespace TvHeadEndM3uProxyService.Controllers
{
    public class TvHeadendController : WebApiController
    {
        private readonly Config.Configuration _config;

        public TvHeadendController(Config.Configuration config)
        {
            _config = config;
        }

        [Route(HttpVerbs.Get, "/tvheadend/channels")]
        public async Task<bool> GetChannelsAsync()
        {
            try
            {
                string remoteUri =
                    $"{_config.TvHeadendAddress}/playlist/channels.m3u";
                string fileName = System.IO.Path.GetTempFileName();
                WebClient myWebClient = new WebClient();
                
                string credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(_config.TvHeadEndUserName + ":" + _config.TvHeadEndPassword));
                myWebClient.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";

                Log.Logger.Information($"Downloading from {remoteUri}");
                myWebClient.DownloadFile(remoteUri, fileName);
                Log.Logger.Information("Download complete");

                Log.Logger.Information("Modifying playlist...");
                var modify = new ModifyPlayList();
                var data = modify.Modify(_config, fileName);
                Log.Logger.Information("Playlist modified");

                using (var writer = HttpContext.OpenResponseText())
                {
                    HttpContext.Response.ContentType = "application/octet-stream";
                    HttpContext.Response.Headers.Add("Content-Description", "File Transfer");
                    HttpContext.Response.Headers.Add("Content-Disposition", "attachment; filename=\"channels.m3u\"");
                    await writer.WriteAsync(data);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Error($"An error has occurred {Environment.NewLine}{e}", e);
                return false;
            }
        }
    }
}
