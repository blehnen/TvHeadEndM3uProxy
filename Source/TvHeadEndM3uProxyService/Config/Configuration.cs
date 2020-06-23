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
using System.ComponentModel;
using ConfOxide;

namespace TvHeadEndM3uProxyService.Config
{
    public sealed class Configuration: SettingsBase<Configuration>
    {
        [DefaultValue("http://+:33721/")]
        public string HostAddress { get; set; }

        public string TvHeadendAddress { get; set; }

        public string TvHeadEndUserName { get; set; }

        public string TvHeadEndPassword { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(HostAddress))
            {
                throw new Exception("HostAddress cannot be null or empty");
            }

            if (string.IsNullOrEmpty(TvHeadendAddress))
            {
                throw new Exception("TvHeadendAddress cannot be null or empty");
            }

            if (string.IsNullOrEmpty(TvHeadEndUserName))
            {
                throw new Exception("TvHeadEndUserName cannot be null or empty");
            }

            if (string.IsNullOrEmpty(TvHeadEndPassword))
            {
                throw new Exception("TvHeadEndPassword cannot be null or empty");
            }
        }
    }
}
