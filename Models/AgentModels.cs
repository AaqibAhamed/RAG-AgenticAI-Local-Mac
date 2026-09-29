namespace RAGOnMyMac.Models;

public enum QueryRoute
{
  AttachedDocuments,
  OfficialDocumentation,
  Combined
}

public sealed record UserQuery(string Text, DateTimeOffset ReceivedAt);

public sealed record TechnologyProfile(
    string Name,
    IReadOnlySet<string> Keywords,
    IReadOnlyList<ResourceCandidate> OfficialResources);

public sealed record ResourceCandidate(
    string Title,
    Uri Uri,
    string SourceType = "official-documentation");

public sealed record EvidenceItem(
    string EvidenceId,
    string Title,
    string Text,
    string SourceType,
    string Citation,
    double Relevance,
    DateTimeOffset RetrievedAt,
    int? PageNumber = null);

public sealed record ResearchResult(
    IReadOnlyList<EvidenceItem> Evidence,
    IReadOnlyList<string> Warnings);

public sealed record AnalysisResult(
    IReadOnlyList<EvidenceItem> Evidence,
    IReadOnlyList<string> Claims,
    IReadOnlyList<string> Warnings);

public sealed record AnswerDraft(
    string Answer,
    IReadOnlyList<EvidenceItem> Evidence,
    string GroundingStatus,
    IReadOnlyList<string> Limitations);

public sealed record AgentExecutionTrace(
    QueryRoute Route,
    IReadOnlyList<string> Agents,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Warnings);

public sealed record KnowledgeAnswer(
    string Text,
    QueryRoute Route,
    string GroundingStatus,
    IReadOnlyList<EvidenceItem> Evidence,
    AgentExecutionTrace Trace);
