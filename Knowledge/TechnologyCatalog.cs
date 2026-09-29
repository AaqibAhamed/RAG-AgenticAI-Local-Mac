using RAGOnMyMac.Configuration;
using RAGOnMyMac.Models;

namespace RAGOnMyMac.Knowledge;

public sealed class TechnologyCatalog(KnowledgeHubOptions options)
{
  private readonly KnowledgeHubOptions _options = options;

  private static readonly TechnologyProfile[] Profiles =
  [
    new(
        ".NET",
        new HashSet<string>([".net", "dotnet", "asp.net", "aspnet", "c#", "csharp"], StringComparer.OrdinalIgnoreCase),
        [
          new("Microsoft Learn .NET", new Uri("https://learn.microsoft.com/en-us/dotnet/")),
          new("ASP.NET Core documentation", new Uri("https://learn.microsoft.com/en-us/aspnet/core/")),
          new("C# documentation", new Uri("https://learn.microsoft.com/en-us/dotnet/csharp/")),
          new(".NET API browser", new Uri("https://learn.microsoft.com/en-us/dotnet/api/"))
        ]),
    new(
        "Angular",
        new HashSet<string>(["angular", "angularjs"], StringComparer.OrdinalIgnoreCase),
        [new("Angular documentation", new Uri("https://angular.dev/overview"))]),
    new(
        "Java",
        new HashSet<string>(["java", "jdk", "jvm", "jakarta"], StringComparer.OrdinalIgnoreCase),
        [
          new("Java documentation", new Uri("https://docs.oracle.com/en/java/")),
          new("Dev.java", new Uri("https://dev.java/learn/"))
        ]),
    new(
        "TypeScript",
        new HashSet<string>(["typescript", "tsconfig"], StringComparer.OrdinalIgnoreCase),
        [new("TypeScript documentation", new Uri("https://www.typescriptlang.org/docs/"))]),
    new(
        "React",
        new HashSet<string>(["react", "jsx", "tsx"], StringComparer.OrdinalIgnoreCase),
        [new("React documentation", new Uri("https://react.dev/learn"))])
  ];

  public IReadOnlyList<TechnologyProfile> Match(string question)
  {
    var normalized = question.ToLowerInvariant();

    return Profiles
        .Where(profile => profile.Keywords.Any(keyword => normalized.Contains(keyword)))
        .Where(profile => profile.OfficialResources.All(resource => IsAllowed(resource.Uri)))
        .ToList();
  }

  private bool IsAllowed(Uri uri) =>
      uri.Scheme is "https" && _options.OfficialDomains.Contains(uri.Host);
}
