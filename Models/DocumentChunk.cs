namespace RAGOnMyMac.Models;

public sealed record DocumentChunk(
    Guid ChunkId,
    string DocumentName,
    int ChunkIndex,
    string Text);
