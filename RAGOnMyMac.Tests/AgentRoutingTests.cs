using RAGOnMyMac.Agents;
using RAGOnMyMac.Models;
using Xunit;

namespace RAGOnMyMac.Tests;

public sealed class AgentRoutingTests
{
    private readonly IQueryRouter _router = new RuleBasedQueryRouter();

    [Fact]
    public void Routes_document_question_to_attached_documents()
    {
        var route = _router.Route(new UserQuery(
            "What does the attached document say about dependency injection?",
            DateTimeOffset.UtcNow));

        Assert.Equal(QueryRoute.AttachedDocuments, route);
    }

    [Fact]
    public void Routes_official_question_to_official_documentation()
    {
        var route = _router.Route(new UserQuery(
            "What is the current official ASP.NET Core documentation for middleware?",
            DateTimeOffset.UtcNow));

        Assert.Equal(QueryRoute.OfficialDocumentation, route);
    }

    [Fact]
    public void Routes_question_referencing_document_and_current_docs_to_combined()
    {
        var route = _router.Route(new UserQuery(
            "Compare the attached document with the latest official API documentation.",
            DateTimeOffset.UtcNow));

        Assert.Equal(QueryRoute.Combined, route);
    }
}
