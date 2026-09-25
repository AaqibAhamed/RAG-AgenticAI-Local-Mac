using RAGOnMyMac.Agents;
using RAGOnMyMac.Models;
using Xunit;

namespace RAGOnMyMac.Tests;

public sealed class ResourceAnalyzerTests
{
    [Fact]
    public void Preserves_evidence_and_warnings()
    {
        var evidence = new EvidenceItem(
            "source-1",
            "Official documentation",
            "A documented API contract.",
            "official-documentation",
            "https://learn.microsoft.com/example",
            0.9,
            DateTimeOffset.UtcNow);
        var result = new ResourceAnalyzer().Analyze(new ResearchResult(
            [evidence],
            ["One source was unavailable."]));

        Assert.Single(result.Evidence);
        Assert.Contains("Official documentation", result.Claims.Single());
        Assert.Contains("One source was unavailable.", result.Warnings);
    }
}
