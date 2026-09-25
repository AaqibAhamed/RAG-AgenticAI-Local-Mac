namespace RAGOnMyMac.Configuration;

public sealed class KnowledgeHubOptions
{
    public string DocumentsPath { get; init; } = "Documents";
    public string OllamaUrl { get; init; } = "http://localhost:11434";
    public string EmbeddingModel { get; init; } = "nomic-embed-text";
    public string GenerativeModel { get; init; } = "llama3.2";
    public string QdrantHost { get; init; } = "localhost";
    public int QdrantPort { get; init; } = 6334;
    public string CollectionName { get; init; } = "rag-on-mac";
    public int ChunkSize { get; init; } = 800;
    public int ChunkOverlap { get; init; } = 100;
    public int RetrievalLimit { get; init; } = 5;
    public IReadOnlySet<string> OfficialDomains { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
  {
    "learn.microsoft.com",
    "dotnet.microsoft.com",
    "docs.microsoft.com"
  };

    public static KnowledgeHubOptions FromEnvironment()
    {
        return new KnowledgeHubOptions
        {
            DocumentsPath = Get("RAG_DOCUMENTS_PATH", "Documents"),
            OllamaUrl = Get("OLLAMA_URL", "http://localhost:11434"),
            EmbeddingModel = Get("OLLAMA_EMBEDDING_MODEL", "nomic-embed-text"),
            GenerativeModel = Get("OLLAMA_GENERATIVE_MODEL", "llama3.2"),
            QdrantHost = Get("QDRANT_HOST", "localhost"),
            QdrantPort = GetInt("QDRANT_PORT", 6334),
            CollectionName = Get("QDRANT_COLLECTION", "rag-on-mac"),
            ChunkSize = GetInt("RAG_CHUNK_SIZE", 800),
            ChunkOverlap = GetInt("RAG_CHUNK_OVERLAP", 100),
            RetrievalLimit = GetInt("RAG_RETRIEVAL_LIMIT", 5),
            OfficialDomains = Get("RAG_OFFICIAL_DOMAINS", "learn.microsoft.com,dotnet.microsoft.com,docs.microsoft.com")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string Get(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
          ? value
          : fallback;

    private static int GetInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value)
          ? value
          : fallback;
}
