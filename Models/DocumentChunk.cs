namespace RAGOnMyMac.Models;

public sealed record DocumentChunk(
    Guid ChunkId,
    string DocumentName,
    int ChunkIndex,
    string Text,
    string SourceType = "attached-document",
    string? SourceUri = null,
    int? PageNumber = null,
    string? Title = null);
