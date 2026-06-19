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
using System.IO;
using System.Text;

namespace TvHeadEndM3uProxyService.Tests
{
    [TestClass]
    public class PlaylistRewriterTests
    {
        private static readonly string FixturesDir = Path.Combine(
            Path.GetDirectoryName(typeof(PlaylistRewriterTests).Assembly.Location)!,
            "Fixtures");

        private const string Username = "user";
        private const string Password = "pass";

        private static byte[] RewriteFixture(string caseName)
        {
            var inputPath = Path.Combine(FixturesDir, $"{caseName}.input.m3u");
            var inputBytes = File.ReadAllBytes(inputPath);
            // Fixtures are UTF-8 without BOM per README
            var inputText = Encoding.UTF8.GetString(inputBytes);
            var rewriter = new PlaylistRewriter();
            var outputText = rewriter.Rewrite(inputText, Username, Password);
            return Encoding.UTF8.GetBytes(outputText);
        }

        private static byte[] ReadExpected(string caseName)
        {
            var expectedPath = Path.Combine(FixturesDir, $"{caseName}.expected.m3u");
            return File.ReadAllBytes(expectedPath);
        }

        [TestMethod]
        public void TicketAndProfile_BytesMatchExpected()
        {
            var actual = RewriteFixture("ticket-and-profile");
            var expected = ReadExpected("ticket-and-profile");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TicketOnly_BytesMatchExpected()
        {
            var actual = RewriteFixture("ticket-only");
            var expected = ReadExpected("ticket-only");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MultiChannel_BytesMatchExpected()
        {
            var actual = RewriteFixture("multi-channel");
            var expected = ReadExpected("multi-channel");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CommentsBlanks_BytesMatchExpected()
        {
            var actual = RewriteFixture("comments-blanks");
            var expected = ReadExpected("comments-blanks");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LfLineEndings_BytesMatchExpected()
        {
            var actual = RewriteFixture("lf");
            var expected = ReadExpected("lf");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CrlfLineEndings_BytesMatchExpected()
        {
            var actual = RewriteFixture("crlf");
            var expected = ReadExpected("crlf");
            CollectionAssert.AreEqual(expected, actual);
        }

        // Regression: drop only the ticket parameter and keep every other parameter
        // (profile plus any trailing params) in order, with a spec-correct leading '?'.
        [TestMethod]
        public void ProfileWithTrailingParams_BytesMatchExpected()
        {
            var actual = RewriteFixture("profile-extra-params");
            var expected = ReadExpected("profile-extra-params");
            CollectionAssert.AreEqual(expected, actual);
        }

        // Regression: credentials containing special characters (space, @, :, /) must
        // be URL-encoded via Uri.EscapeDataString so the rewritten URL is valid.
        // username "us er" -> "us%20er"; password "p@ss:w/rd" -> "p%40ss%3Aw%2Frd".
        [TestMethod]
        public void SpecialCharCredentials_AreUrlEncoded()
        {
            var inputPath = Path.Combine(FixturesDir, "special-char-creds.input.m3u");
            var inputBytes = File.ReadAllBytes(inputPath);
            var inputText = Encoding.UTF8.GetString(inputBytes);
            var rewriter = new PlaylistRewriter();
            var outputText = rewriter.Rewrite(inputText, "us er", "p@ss:w/rd");
            var actual = Encoding.UTF8.GetBytes(outputText);
            var expected = File.ReadAllBytes(Path.Combine(FixturesDir, "special-char-creds.expected.m3u"));
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
