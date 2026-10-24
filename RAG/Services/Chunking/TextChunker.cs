using RAG.Models;

namespace RAG.Services.Chunking
{
    public sealed class TextChunker : ITextChunker
    {
        public List<DocumentChunk> FixedSize(string text, Guid sourceDocumentId, string sourceFileName,       int chunkSize,
            int overlap)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chunkSize),
                    "Chunk size must be greater than zero.");
            }

            if (overlap < 0 || overlap >= chunkSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(overlap),
                    "Overlap must be non-negative and smaller than chunk size.");
            }

            var chunks = new List<DocumentChunk>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            int start = 0;
            int chunkIndex = 0;

            while (start < text.Length)
            {
                int length = Math.Min(chunkSize,text.Length - start);

                string chunkText = text.Substring(start, length);

                chunks.Add(new DocumentChunk
                {
                    ChunkId = Guid.NewGuid(),
                    SourceDocumentId = sourceDocumentId,
                    SourceFileName = sourceFileName,
                    ChunkIndex = chunkIndex,
                    Text = chunkText
                });

                chunkIndex++;

                if (start + length >= text.Length)
                {
                    break;
                }

                start += chunkSize - overlap;
            }

            return chunks;
        }

 public List<DocumentChunk> SentenceBased(string text,Guid sourceDocumentId,
    string sourceFileName, int maxChunkCharacters)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (maxChunkCharacters <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxChunkCharacters));
            }

            var chunks = new List<DocumentChunk>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            var sentences = System.Text.RegularExpressions.Regex
                .Split(text.Trim(), @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s));

            var current = new System.Text.StringBuilder();

            void SaveChunk()
            {
                var chunkText = current.ToString().Trim();

                if (chunkText.Length == 0)
                {
                    return;
                }

                chunks.Add(new DocumentChunk
                {
                    SourceDocumentId = sourceDocumentId,
                    SourceFileName = sourceFileName,
                    ChunkIndex = chunks.Count,
                    Text = chunkText
                });

                current.Clear();
            }

            foreach (var sentence in sentences)
            {
                var candidate = current.Length == 0
                    ? sentence
                    : current + " " + sentence;

                if (candidate.Length <= maxChunkCharacters)
                {
                    if (current.Length > 0)
                    {
                        current.Append(' ');
                    }

                    current.Append(sentence);
                    continue;
                }

                SaveChunk();

                if (sentence.Length <= maxChunkCharacters)
                {
                    current.Append(sentence);
                }
                else
                {
                    // Fallback for an unusually long sentence.
                    foreach (var piece in FixedSize(
                        sentence,
                        sourceDocumentId,
                        sourceFileName,
                        maxChunkCharacters,
                        0))
                    {
                        piece.ChunkIndex = chunks.Count;
                        chunks.Add(piece);
                    }
                }
            }

            SaveChunk();

            return chunks;
        }


        public List<DocumentChunk> ParagraphBased(string text, Guid sourceDocumentId,string sourceFileName, int maxChunkCharacters)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (maxChunkCharacters <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxChunkCharacters));
            }

            var chunks = new List<DocumentChunk>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            var paragraphs = System.Text.RegularExpressions.Regex
                .Split(text.Trim(), @"\n\s*\n")
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var current = new System.Text.StringBuilder();

            void SaveChunk()
            {
                var value = current.ToString().Trim();

                if (value.Length == 0)
                {
                    return;
                }

                chunks.Add(new DocumentChunk
                {
                    SourceDocumentId = sourceDocumentId,
                    SourceFileName = sourceFileName,
                    ChunkIndex = chunks.Count,
                    Text = value
                });

                current.Clear();
            }

            foreach (var paragraph in paragraphs)
            {
                var candidate = current.Length == 0
                    ? paragraph
                    : current + "\n\n" + paragraph;

                if (candidate.Length <= maxChunkCharacters)
                {
                    if (current.Length > 0)
                    {
                        current.Append("\n\n");
                    }

                    current.Append(paragraph);
                    continue;
                }

                SaveChunk();

                if (paragraph.Length <= maxChunkCharacters)
                {
                    current.Append(paragraph);
                }
                else
                {
                    // A single paragraph is too large.
                    // Fall back to sentence-based chunking.
                    var pieces = SentenceBased(
                        paragraph,
                        sourceDocumentId,
                        sourceFileName,
                        maxChunkCharacters);

                    foreach (var piece in pieces)
                    {
                        piece.ChunkIndex = chunks.Count;
                        chunks.Add(piece);
                    }
                }
            }

            SaveChunk();

            return chunks;
        }


        public List<DocumentChunk> Recursive(
    string text,
    Guid sourceDocumentId,
    string sourceFileName,
    int maxChunkCharacters,
    int overlap = 0)
        {
            ArgumentNullException.ThrowIfNull(text);

            if (maxChunkCharacters <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxChunkCharacters));
            }

            if (overlap < 0 || overlap >= maxChunkCharacters)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(overlap));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<DocumentChunk>();
            }

            var pieces = SplitRecursively(
                text.Trim(),
                maxChunkCharacters);

            var chunks = new List<DocumentChunk>();

            string previousText = string.Empty;

            foreach (var piece in pieces)
            {
                var chunkText = piece.Trim();

                if (chunkText.Length == 0)
                {
                    continue;
                }

                if (overlap > 0 &&
                    chunks.Count > 0 &&
                    previousText.Length > 0)
                {
                    var overlapText = previousText[
                        Math.Max(0, previousText.Length - overlap)..];

                    // Avoid exceeding the configured character limit.
                    var available = maxChunkCharacters - chunkText.Length;

                    if (available > 0)
                    {
                        if (overlapText.Length > available)
                        {
                            overlapText = overlapText[^available..];
                        }

                        chunkText = overlapText + chunkText;
                    }
                }

                chunks.Add(new DocumentChunk
                {
                    SourceDocumentId = sourceDocumentId,
                    SourceFileName = sourceFileName,
                    ChunkIndex = chunks.Count,
                    Text = chunkText
                });

                previousText = piece.Trim();
            }

            return chunks;
        }

        private static List<string> SplitRecursively(string text,int maxChunkCharacters)
        {
            if (text.Length <= maxChunkCharacters)
            {
                return new List<string> { text };
            }

            string[] separators ={
        "\n\n",
        "\n",
        ". ",
        " ",
        ""
    };

            foreach (var separator in separators)
            {
                if (separator.Length == 0)
                {
                    return SplitByFixedSize(
                        text,
                        maxChunkCharacters);
                }

                if (!text.Contains(separator))
                {
                    continue;
                }

                var parts = text.Split(
                    new[] { separator },
                    StringSplitOptions.None);

                var result = new List<string>();
                var current = new System.Text.StringBuilder();

                foreach (var part in parts)
                {
                    var candidate = current.Length == 0
                        ? part
                        : current + separator + part;

                    if (candidate.Length <= maxChunkCharacters)
                    {
                        if (current.Length > 0)
                        {
                            current.Append(separator);
                        }

                        current.Append(part);
                        continue;
                    }

                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }

                    if (part.Length > maxChunkCharacters)
                    {
                        result.AddRange(
                            SplitRecursively(
                                part,
                                maxChunkCharacters));
                    }
                    else
                    {
                        current.Append(part);
                    }
                }

                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                }

                if (result.Count > 0)
                {
                    return result;
                }
            }

            return SplitByFixedSize(text, maxChunkCharacters);
        }

        private static List<string> SplitByFixedSize(string text,
            int maxChunkCharacters)
        {
            var result = new List<string>();

            for (int start = 0; start < text.Length;)
            {
                int length = Math.Min(
                    maxChunkCharacters,
                    text.Length - start);

                result.Add(text.Substring(start, length));

                start += length;
            }

            return result;
        }

        public List<DocumentChunk> ChunkPages(
    IEnumerable<DocumentPage> pages,
    int maxChunkCharacters)
        {
            var result = new List<DocumentChunk>();

            foreach (var page in pages.OrderBy(p => p.PageNumber))
            {
                var pageChunks = Recursive(
                    page.Text,
                    page.SourceDocumentId,
                    page.SourceFileName,
                    maxChunkCharacters);

                foreach (var chunk in pageChunks)
                {
                    chunk.ChunkIndex = result.Count;
                    chunk.PageNumber = page.PageNumber;

                    result.Add(chunk);
                }
            }

            return result;
        }


    }

}
