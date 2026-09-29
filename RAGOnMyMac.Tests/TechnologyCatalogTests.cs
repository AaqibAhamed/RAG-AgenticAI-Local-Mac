using RAGOnMyMac.Configuration;
using RAGOnMyMac.Knowledge;
using Xunit;

namespace RAGOnMyMac.Tests;

public sealed class TechnologyCatalogTests
{
  private readonly TechnologyCatalog _catalog =
      new(KnowledgeHubOptions.FromEnvironment());

  [Fact]
  public void Angular_questions_select_angular_official_documentation()
  {
    var profile = Assert.Single(_catalog.Match("How do I create an Angular service?"));

    Assert.Equal("Angular", profile.Name);
    Assert.All(profile.OfficialResources, resource =>
        Assert.Equal("angular.dev", resource.Uri.Host));
  }

  [Fact]
  public void Java_questions_select_oracle_and_dev_java_documentation()
  {
    var profile = Assert.Single(_catalog.Match("Explain Java virtual threads using official documentation."));

    Assert.Equal("Java", profile.Name);
    Assert.Contains(profile.OfficialResources, resource => resource.Uri.Host == "docs.oracle.com");
    Assert.Contains(profile.OfficialResources, resource => resource.Uri.Host == "dev.java");
  }
}