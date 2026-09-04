using RAGOnMyMac.Models;

internal interface IOllamaService
{
  public Task<float[]> GenerateEmbeddingAsync(
    string text, CancellationToken cancellationToken = default);

  public Task<string> GenerateAnswerAsync(
    string question, IReadOnlyList<SearchResult> results, CancellationToken cancellationToken = default);
}
