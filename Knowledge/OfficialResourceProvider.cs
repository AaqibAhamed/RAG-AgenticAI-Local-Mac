using System.Net;
using System.Text.RegularExpressions;
using RAGOnMyMac.Configuration;
using RAGOnMyMac.Models;

namespace RAGOnMyMac.Knowledge;

public interface IOfficialResourceProvider
{
    Task<ResearchResult> ResearchAsync(
        UserQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class OfficialResourceProvider(
    HttpClient httpClient,
    KnowledgeHubOptions options) : IOfficialResourceProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly KnowledgeHubOptions _options = options;

    private static readonly ResourceCandidate[] Catalog =
    [
      new("Microsoft Learn", new Uri("https://learn.microsoft.com/en-us/dotnet/")),
    new("ASP.NET Core documentation", new Uri("https://learn.microsoft.com/en-us/aspnet/core/")),
    new("C# documentation", new Uri("https://learn.microsoft.com/en-us/dotnet/csharp/")),
    new(".NET API browser", new Uri("https://learn.microsoft.com/en-us/dotnet/api/")),
    new(".NET documentation", new Uri("https://dotnet.microsoft.com/en-us/learn/dotnet/what-is-dotnet"))
    ];

    public async Task<ResearchResult> ResearchAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        var candidates = SelectCandidates(query.Text);
        var evidence = new List<EvidenceItem>();
        var warnings = new List<string>();

        foreach (var candidate in candidates)
        {
            if (!IsAllowed(candidate.Uri))
            {
                warnings.Add($"Skipped non-allowlisted source: {candidate.Uri.Host}");
                continue;
            }

            try
            {
                using var response = await _httpClient.GetAsync(
                    candidate.Uri,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    warnings.Add($"Source returned {(int)response.StatusCode}: {candidate.Uri}");
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var text = ExtractText(html);
                if (text.Length == 0)
                {
                    warnings.Add($"Source contained no readable text: {candidate.Uri}");
                    continue;
                }

                evidence.Add(new EvidenceItem(
                    candidate.Uri.AbsoluteUri,
                    candidate.Title,
                    TrimEvidence(text),
                    candidate.SourceType,
                    candidate.Uri.AbsoluteUri,
                    0.75,
                    DateTimeOffset.UtcNow));
            }
            catch (HttpRequestException exception)
            {
                warnings.Add($"Failed to fetch {candidate.Uri}: {exception.Message}");
            }
        }

        return new ResearchResult(evidence, warnings);
    }

    private IReadOnlyList<ResourceCandidate> SelectCandidates(string question)
    {
        var normalized = question.ToLowerInvariant();
        return Catalog
            .Where(candidate =>
                normalized.Contains("asp.net") && candidate.Title.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("c#") && candidate.Title.Contains("C#", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("api") && candidate.Title.Contains("API", StringComparison.OrdinalIgnoreCase)
                || candidate.Title is "Microsoft Learn" or ".NET documentation")
            .Take(3)
            .ToList();
    }

    private bool IsAllowed(Uri uri) =>
        uri.Scheme is "https" &&
        _options.OfficialDomains.Contains(uri.Host);

    private static string ExtractText(string html)
    {
        var withoutScripts = Regex.Replace(
            html,
            "<(script|style)[^>]*>.*?</\\1>",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var withoutTags = Regex.Replace(withoutScripts, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(Regex.Replace(withoutTags, "\\s+", " ")).Trim();
    }

    private static string TrimEvidence(string text) =>
        text.Length <= 6000 ? text : text[..6000];
}
