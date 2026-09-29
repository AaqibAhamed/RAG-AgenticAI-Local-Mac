using RAGOnMyMac.Knowledge;
using RAGOnMyMac.Models;

namespace RAGOnMyMac.Agents;

public sealed class KnowledgeCoordinator(
    IQueryRouter router,
    IOfficialResourceProvider resourceProvider,
    IResourceAnalyzer resourceAnalyzer,
    OllamaService ollama,
    QdrantVectorStore vectorStore,
    int retrievalLimit,
    SemanticKernelAgentOrchestrator orchestrator)
{
  private readonly IQueryRouter _router = router;
  private readonly IOfficialResourceProvider _resourceProvider = resourceProvider;
  private readonly IResourceAnalyzer _resourceAnalyzer = resourceAnalyzer;
  private readonly OllamaService _ollama = ollama;
  private readonly QdrantVectorStore _vectorStore = vectorStore;
  private readonly int _retrievalLimit = retrievalLimit;
  private readonly SemanticKernelAgentOrchestrator _orchestrator = orchestrator;

  public async Task<KnowledgeAnswer> AnswerAsync(
      string question,
      CancellationToken cancellationToken = default)
  {
    return await _orchestrator.ExecuteAsync(
        () => AnswerCoreAsync(question, cancellationToken),
        cancellationToken);
  }

  private async Task<KnowledgeAnswer> AnswerCoreAsync(
      string question,
      CancellationToken cancellationToken)
  {
    var query = new UserQuery(question, DateTimeOffset.UtcNow);
    var route = _router.Route(query);
    var evidence = new List<EvidenceItem>();
    var warnings = new List<string>();
    var agents = new List<string> { nameof(RuleBasedQueryRouter) };

    if (route is QueryRoute.AttachedDocuments or QueryRoute.Combined)
    {
      var questionEmbedding = await _ollama.GenerateEmbeddingAsync(
          question,
          cancellationToken);
      var results = await _vectorStore.SearchAsync(
          questionEmbedding,
          (ulong)_retrievalLimit,
          cancellationToken);

      evidence.AddRange(results.Select(result => new EvidenceItem(
          $"{result.DocumentName}:{result.ChunkIndex}",
          result.Title ?? result.DocumentName,
          result.Text,
          result.SourceType,
          FormatDocumentCitation(result),
          result.Score,
          DateTimeOffset.UtcNow,
          result.PageNumber)));
      agents.Add(nameof(QdrantVectorStore));
    }

    if (route is QueryRoute.OfficialDocumentation or QueryRoute.Combined)
    {
      var research = await _resourceProvider.ResearchAsync(query, cancellationToken);
      var analysis = _resourceAnalyzer.Analyze(research);
      evidence.AddRange(analysis.Evidence);
      warnings.AddRange(analysis.Warnings);
      agents.Add(nameof(OfficialResourceProvider));
      agents.Add(nameof(ResourceAnalyzer));
    }

    var answer = await _ollama.GenerateGroundedAnswerAsync(
        question,
        evidence,
        cancellationToken);
    var groundingStatus = evidence.Count == 0 ? "insufficient-evidence" : "grounded";
    var sources = evidence.Select(item => item.Citation).Distinct().ToList();
    var trace = new AgentExecutionTrace(route, agents, sources, warnings);

    return new KnowledgeAnswer(answer, route, groundingStatus, evidence, trace);
  }

  private static string FormatDocumentCitation(SearchResult result) =>
      result.PageNumber is { } page
        ? $"{result.DocumentName}, page {page}"
        : result.DocumentName;
}
