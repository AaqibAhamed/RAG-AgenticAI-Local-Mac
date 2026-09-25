using Qdrant.Client;
using Qdrant.Client.Grpc;
using RAGOnMyMac.Models;

namespace RAGOnMyMac;

public sealed class QdrantVectorStore(QdrantClient client, string collectionName)
{
  private readonly QdrantClient _client = client;
  private readonly string _collectionName = collectionName;

  public async Task EnsureCollectionAsync(
    ulong vectorSize, CancellationToken cancellationToken = default)
  {
    if (await _client.CollectionExistsAsync(_collectionName, cancellationToken))
    {
      return;
    }

    await _client.CreateCollectionAsync(
        collectionName: _collectionName,
        vectorsConfig: new VectorParams
        {
          Size = vectorSize,
          Distance = Distance.Cosine
        },
        cancellationToken: cancellationToken);
  }

  public async Task StoreAsync(
    DocumentChunk chunk, float[] embedding, CancellationToken cancellationToken = default)
  {
    var point = new PointStruct
    {
      Id = new PointId
      {
        Uuid = chunk.ChunkId.ToString()
      },
      Vectors = embedding,
      Payload =
        {
            ["documentName"] = chunk.DocumentName,
            ["chunkIndex"] = chunk.ChunkIndex,
            ["text"] = chunk.Text,
            ["sourceType"] = chunk.SourceType,
            ["sourceUri"] = chunk.SourceUri ?? string.Empty,
            ["pageNumber"] = chunk.PageNumber ?? 0,
            ["title"] = chunk.Title ?? chunk.DocumentName
        }
    };

    await _client.UpsertAsync(
        _collectionName,
        [point],
        cancellationToken: cancellationToken);
  }

  public async Task<IReadOnlyList<SearchResult>> SearchAsync(
    float[] queryVector, ulong limit = 5, CancellationToken cancellationToken = default)
  {
    var results = await _client.QueryAsync(
        collectionName: _collectionName,
        query: queryVector,
        limit: limit,
        cancellationToken: cancellationToken);

    return [.. results
        .Select(result => new SearchResult(
          result.Payload["documentName"].StringValue,
          (int)result.Payload["chunkIndex"].IntegerValue,
          result.Payload["text"].StringValue,
          result.Score,
                ReadString(result.Payload, "sourceType", "attached-document") ?? "attached-document",
          ReadString(result.Payload, "sourceUri", null),
          ReadPageNumber(result.Payload),
          ReadString(result.Payload, "title", null)))];
  }

  private static string? ReadString(
    Google.Protobuf.Collections.MapField<string, Value> payload,
    string key,
    string? fallback)
  {
    return payload.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.StringValue)
      ? value.StringValue
      : fallback;
  }

  private static int? ReadPageNumber(
    Google.Protobuf.Collections.MapField<string, Value> payload)
  {
    return payload.TryGetValue("pageNumber", out var value) && value.IntegerValue > 0
      ? (int)value.IntegerValue
      : null;
  }
}
