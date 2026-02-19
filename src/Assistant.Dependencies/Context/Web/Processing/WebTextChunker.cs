using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.Dependencies.Context.Web.Processing
{
    public static class WebTextChunker
    {
        public sealed class Options
        {
            public int MaximumChunkCharacters { get; init; } = 1000;
            public int MinimumChunkCharacters { get; init; } = 200;
            public int MaximumLinesPerChunk { get; init; } = 50;

            public Options()
            {
            }
        }

        public static IReadOnlyList<string> SplitIntoChunks(string extractedText, Options? options = null)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
                return Array.Empty<string>();

            Options effectiveOptions = options ?? new Options();

            string normalizedText = NormalizeNewlines(extractedText);

            IReadOnlyList<string> logicalBlocks = SplitIntoBlocks(normalizedText);

            var chunks = new List<string>();

            foreach (string block in logicalBlocks)
            {
                if (string.IsNullOrWhiteSpace(block))
                    continue;

                AppendBlockAsChunks(block, effectiveOptions, chunks);
            }

            return chunks;
        }

        private static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static IReadOnlyList<string> SplitIntoBlocks(string normalizedText)
        {
            string[] lines = normalizedText.Split('\n');

            var blocks = new List<string>();
            var currentBlockLines = new List<string>();

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushBlock(blocks, currentBlockLines);
                    continue;
                }

                currentBlockLines.Add(line);
            }

            FlushBlock(blocks, currentBlockLines);

            return blocks;
        }

        private static void FlushBlock(List<string> blocks, List<string> currentBlockLines)
        {
            if (currentBlockLines.Count == 0)
                return;

            string block = string.Join("\n", currentBlockLines).Trim();
            if (!string.IsNullOrWhiteSpace(block))
                blocks.Add(block);

            currentBlockLines.Clear();
        }

        private static void AppendBlockAsChunks(
            string block,
            Options options,
            List<string> chunks)
        {
            IReadOnlyList<string> lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var currentChunk = new StringBuilder(capacity: Math.Min(block.Length, options.MaximumChunkCharacters));
            int currentLineCount = 0;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                bool isHeadingBoundary = IsHeadingLikeLine(line);

                bool wouldExceedLineLimit = currentLineCount >= options.MaximumLinesPerChunk;
                bool wouldExceedCharLimit = currentChunk.Length + line.Length + 1 > options.MaximumChunkCharacters;

                if ((isHeadingBoundary && currentChunk.Length > 0) || wouldExceedLineLimit || wouldExceedCharLimit)
                {
                    FlushChunkIfUseful(chunks, currentChunk, options);
                    currentLineCount = 0;
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append('\n');

                currentChunk.Append(line);
                currentLineCount++;
            }

            FlushChunkIfUseful(chunks, currentChunk, options);
        }

        private static void FlushChunkIfUseful(
            List<string> chunks,
            StringBuilder currentChunk,
            Options options)
        {
            if (currentChunk.Length == 0)
                return;

            string chunk = currentChunk.ToString().Trim();
            currentChunk.Clear();

            if (chunk.Length < options.MinimumChunkCharacters)
                return;

            chunks.Add(chunk);
        }

        private static bool IsHeadingLikeLine(string line)
        {
            if (line.Length <= 2)
                return false;

            if (line.Length <= 40 && line.EndsWith(":", StringComparison.Ordinal))
                return true;

            if (line.Length <= 30 && IsMostlyUppercaseLetters(line))
                return true;

            return false;
        }

        private static bool IsMostlyUppercaseLetters(string line)
        {
            int letterCount = 0;
            int uppercaseLetterCount = 0;

            foreach (char character in line)
            {
                if (!char.IsLetter(character))
                    continue;

                letterCount++;

                if (char.IsUpper(character))
                    uppercaseLetterCount++;
            }

            if (letterCount == 0)
                return false;

            return uppercaseLetterCount * 100 / letterCount >= 70;
        }
    }
}
