using RAGOnMyMac.Models;

namespace RAGOnMyMac.Agents;

public interface IQueryRouter
{
    QueryRoute Route(UserQuery query);
}

public sealed class RuleBasedQueryRouter : IQueryRouter
{
    public QueryRoute Route(UserQuery query)
    {
        var text = query.Text.ToLowerInvariant();
        var referencesDocument =
            text.Contains("attached") ||
            text.Contains("this document") ||
            text.Contains("this pdf") ||
            text.Contains("in the document");
        var requestsCurrentDocumentation =
            text.Contains("official") ||
            text.Contains("latest") ||
            text.Contains("current") ||
            text.Contains("documentation") ||
            text.Contains("api reference");

        return (referencesDocument, requestsCurrentDocumentation) switch
        {
            (true, true) => QueryRoute.Combined,
            (true, false) => QueryRoute.AttachedDocuments,
            _ => QueryRoute.OfficialDocumentation
        };
    }
}

public interface IResourceAnalyzer
{
    AnalysisResult Analyze(ResearchResult research);
}

public sealed class ResourceAnalyzer : IResourceAnalyzer
{
    public AnalysisResult Analyze(ResearchResult research)
    {
        var claims = research.Evidence
            .Select(evidence => $"{evidence.Title} provides information relevant to the user question.")
            .ToList();

        return new AnalysisResult(research.Evidence, claims, research.Warnings);
    }
}
