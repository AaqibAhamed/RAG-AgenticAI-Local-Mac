namespace RAGOnMyMac.Models;

public sealed record SearchResult(
    string DocumentName,
    int ChunkIndex,
    string Text,
    float Score,
    string SourceType = "attached-document",
    string? SourceUri = null,
    int? PageNumber = null,
    string? Title = null);
