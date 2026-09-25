using System.Security.Cryptography;
using System.Text;
using RAGOnMyMac.Configuration;
using RAGOnMyMac.Models;
using UglyToad.PdfPig;

namespace RAGOnMyMac.Knowledge;

public sealed class PdfKnowledgeSource(KnowledgeHubOptions options)
{
    private readonly KnowledgeHubOptions _options = options;

    public IReadOnlyList<DocumentChunk> LoadChunks()
    {
        if (!Directory.Exists(_options.DocumentsPath))
        {
            throw new DirectoryNotFoundException(
                $"The documents directory '{_options.DocumentsPath}' does not exist.");
        }

        var files = Directory.GetFiles(
            _options.DocumentsPath,
            "*.pdf",
            SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            throw new FileNotFoundException(
                $"Place at least one PDF file in '{_options.DocumentsPath}'.");
        }

        return files
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .SelectMany(LoadFile)
            .ToList();
    }

    private IReadOnlyList<DocumentChunk> LoadFile(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        var documentName = Path.GetFileName(filePath);
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        foreach (var page in pdf.GetPages())
        {
            foreach (var text in SplitText(page.Text))
            {
                chunks.Add(new DocumentChunk(
                    CreateStableChunkId(documentName, page.Number, chunkIndex, text),
                    documentName,
                    chunkIndex++,
                    text,
                    PageNumber: page.Number,
                    SourceUri: Path.GetFullPath(filePath),
                    Title: documentName));
            }

        }

        return chunks;
    }

    private static Guid CreateStableChunkId(
        string documentName,
        int pageNumber,
        int chunkIndex,
        string text)
    {
        var value = Encoding.UTF8.GetBytes(
            $"{documentName}|{pageNumber}|{chunkIndex}|{text}");
        var hash = SHA256.HashData(value);
        return new Guid(hash[..16]);
    }

    private IEnumerable<string> SplitText(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(_options.ChunkSize, text.Length - start);
            var chunk = text.Substring(start, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                yield return chunk;
            }

            if (start + length >= text.Length)
            {
                yield break;
            }

            start += Math.Max(1, length - _options.ChunkOverlap);
        }
    }
}
