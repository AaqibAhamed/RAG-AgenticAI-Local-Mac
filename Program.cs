using Qdrant.Client;
using RAGOnMyMac;
using RAGOnMyMac.Agents;
using RAGOnMyMac.Configuration;
using RAGOnMyMac.Knowledge;

var options = KnowledgeHubOptions.FromEnvironment();

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(options.OllamaUrl),
    Timeout = TimeSpan.FromSeconds(45)
};

var ollama = new OllamaService(
        httpClient,
        options.EmbeddingModel,
        options.GenerativeModel);

var qdrantClient = new QdrantClient(
        options.QdrantHost,
        options.QdrantPort);

var vectorStore = new QdrantVectorStore(
        qdrantClient,
        options.CollectionName);

var documentSource = new PdfKnowledgeSource(options);
var chunks = documentSource.LoadChunks();

if (chunks.Count == 0)
{
    throw new InvalidOperationException(
            "The configured PDF documents contain no readable text.");
}

var firstEmbedding = await ollama.GenerateEmbeddingAsync(chunks[0].Text);
await vectorStore.EnsureCollectionAsync((ulong)firstEmbedding.Length);
await vectorStore.StoreAsync(chunks[0], firstEmbedding);

foreach (var chunk in chunks.Skip(1))
{
    var embedding = await ollama.GenerateEmbeddingAsync(chunk.Text);
    await vectorStore.StoreAsync(chunk, embedding);
}

Console.WriteLine($"Indexed {chunks.Count} document chunks from the configured knowledge base.");
Console.WriteLine("Agentic developer knowledge hub ready.");
Console.WriteLine("Ask a question, or type 'exit' to quit.");

var coordinator = new KnowledgeCoordinator(
        new RuleBasedQueryRouter(),
        new OfficialResourceProvider(httpClient, options),
        new ResourceAnalyzer(),
        ollama,
        vectorStore,
        options.RetrievalLimit);

while (true)
{
    Console.Write("\nYou: ");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        continue;
    }

    if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var result = await coordinator.AnswerAsync(question);

        Console.WriteLine();
        Console.WriteLine($"Route: {result.Route}");
        Console.WriteLine($"Grounding: {result.GroundingStatus}");
        Console.WriteLine($"Assistant: {result.Text}");

        if (result.Evidence.Count > 0)
        {
            Console.WriteLine("\nSources:");
            foreach (var source in result.Evidence.Select(item => item.Citation).Distinct())
            {
                Console.WriteLine($"- {source}");
            }
        }

        foreach (var warning in result.Trace.Warnings)
        {
            Console.WriteLine($"Warning: {warning}");
        }
    }
    catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
    {
        Console.WriteLine($"Unable to answer: {exception.Message}");
    }
}
