using System.Net.Http.Json;
using RAGOnMyMac.Models;

namespace RAGOnMyMac;

public sealed class OllamaService(
  HttpClient httpClient, string embeddingModel, string generativeModel) : IOllamaService
{
  private readonly HttpClient _httpClient = httpClient;
  private readonly string _embeddingModel = embeddingModel;
  private readonly string _generativeModel = generativeModel;

  public async Task<float[]> GenerateEmbeddingAsync(
    string text, CancellationToken cancellationToken = default)
  {
    var request = new
    {
      model = _embeddingModel,
      input = text
    };

    using var response = await _httpClient.PostAsJsonAsync(
        "/api/embed",
        request,
        cancellationToken);

    response.EnsureSuccessStatusCode();

    var result = await response.Content
        .ReadFromJsonAsync<OllamaEmbeddingResponse>(
            cancellationToken);

    return result?.Embeddings.FirstOrDefault()
        ?? throw new InvalidOperationException(
            "Ollama returned no embedding.");
  }

  public async Task<string> GenerateAnswerAsync(
    string question, IReadOnlyList<SearchResult> results, CancellationToken cancellationToken = default)
  {
    var context = string.Join(
        "\n\n",
        results.Select(result => result.Text));

    var prompt = $"""
        Answer the question using only the provided context.

        If the context contains enough information to answer the question,
        provide a direct answer based on that information.

        Only say that there is not enough information when the context
        does not contain the answer.

        Do not speculate or add information that is not present in the context.

        Context:
        {context}

        Question:
        {question}
        """;

    var request = new
    {
      model = _generativeModel,
      prompt,
      stream = false
    };

    using var response = await _httpClient.PostAsJsonAsync(
        "/api/generate",
        request,
        cancellationToken);

    response.EnsureSuccessStatusCode();

    var result = await response.Content
        .ReadFromJsonAsync<OllamaGenerateResponse>(
            cancellationToken);

    return result?.Response
        ?? throw new InvalidOperationException(
            "Ollama returned no response.");
  }
}
