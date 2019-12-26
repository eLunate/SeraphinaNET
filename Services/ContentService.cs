using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SeraphinaNET.Data;
using System.Linq;
using Discord;

namespace SeraphinaNET.Services {
    // Does this really have to be a service?
    // I suppose yes because it needs activity metrics to get weighted scores.
    // It's just a really shitty service name, for lack of a more descriptive one.
    class ContentService {
        public struct ContentScore {
            public readonly uint TextScore;
            public readonly uint AltScore;

            public ContentScore(uint textScore, uint altScore) {
                this.TextScore = textScore;
                this.AltScore = altScore;
            }
        }

        private readonly DataContextFactory data;

        public ContentService(DataContextFactory data) {
            this.data = data;
        }

        private static readonly Regex urlRegex = new Regex(@"(?>https?://\S+)");
        private static readonly Regex mentionRegex = new Regex(@"(?:@(?<inner>here|everyone)|<(?:@!?|[#&])(?<inner>\d+)>)");
        private static readonly Regex codeRegex = new Regex("(?:`(?<inner>[^`]+)`|```[^\n]+\n(?<inner>.+?)\n```)");
        private static readonly Regex formattingSymbolsRegex = new Regex(@"(?<![\\])(?:(?<!\s)[*_|]|[*_|](?!\s))"); // I really don't think I can [be bothered to] do it properly.
        private static readonly Regex wordRegex = new Regex(@"\w+");

        public static string WithoutUrls(string input) => urlRegex.Replace(input, "");
        public static string WithoutMentions(string input) => mentionRegex.Replace(input, "");
        public static string WithoutCode(string input) => codeRegex.Replace(input, "");
        public static string WithoutFormatting(string input) => formattingSymbolsRegex.Replace(input, "");

        public uint ScoreText(string text) {
            // Need to break down the text score into its components.
            // Forget about performance for now. You can care about it later. Just get it done.
            var words = wordRegex.Matches(text).Select(x => x.Value);

            var wordCount = words.Count();
            var totalWordContent = words.Select(x => x.Length).Sum();
            var avgWordLength = (double)totalWordContent / wordCount;

            // Arbitrary scoring to the rescue!
            return (uint)(Math.Pow(avgWordLength, 1.4) * wordCount);
        }

        // This should be an enum or class (or some shit) indicating the reason the text is not allowed
        public bool CheckTextSimple(string text, double trustFactor = 1) {
            // True = Allow; False = fail

            // If the trustfactor is more than a threshold, ignore code for simple spam calculations.
            text = WithoutCode(text);

            // These stats could potentially be reused from a previous calculation, such as from ScoreText
            // Consider refactoring later to take some text stats struct instead.
            var words = wordRegex.Matches(text).Select(x => x.Value);
            var wordContent = words.Select(x => x.Length).Sum();
            var wordCount = words.Count();

            // Ensure that average word length is over 5+trust, bias by 1 word for short messages.
            return (wordContent <= (wordCount + 1) * (5 + trustFactor))
                // Ensure that average word length is over 2, bias by 3 words to be really nice.
                && (wordContent > (wordCount - 3) * 2)
                // Ensure there's not more mentions than the trust count
                && mentionRegex.Matches(text).Count() <= trustFactor;
        }

        public bool CheckMessage(IUserMessage message) { // False = fail
            if (message.Source != MessageSource.User) return true; // Ignore bots.
            return CheckTextSimple(message.Content);
        }

        public ContentScore ScoreMessage(IUserMessage message) {
            if (message.Source != MessageSource.User) return new ContentScore(); // No bots.
            return new ContentScore(
                textScore: ScoreText(WithoutFormatting(WithoutUrls(WithoutMentions(WithoutCode(message.Content))))),
                altScore: (uint)message.Attachments.Count() * 50
            ); // Why would Count have an int? Surely a quantity is a natural number.
        }
    }
}
