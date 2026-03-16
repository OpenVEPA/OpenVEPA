namespace OpenVEPA.Core.Storage;

/// <summary>Provides generic document persistence with collection-based partitioning.</summary>
public interface IDocumentStore
{
    /// <summary>Gets a document by collection and id, or null if not found.</summary>
    Task<T?> GetAsync<T>(string collection, string id, CancellationToken ct) where T : class;

    /// <summary>Creates or updates a document in the specified collection.</summary>
    Task UpsertAsync<T>(string collection, string id, T document, CancellationToken ct) where T : class;

    /// <summary>Deletes a document from the specified collection.</summary>
    Task DeleteAsync(string collection, string id, CancellationToken ct);

    /// <summary>Enumerates all documents in the specified collection.</summary>
    IAsyncEnumerable<T> QueryAsync<T>(string collection, CancellationToken ct) where T : class;
}
