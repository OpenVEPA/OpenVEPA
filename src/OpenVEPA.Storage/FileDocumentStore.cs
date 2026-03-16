using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenVEPA.Core.Storage;

namespace OpenVEPA.Storage;

/// <summary>
/// File-system backed document store. Each document is a JSON file
/// stored at <c>{basePath}/{collection}/{id}.json</c>.
/// </summary>
public sealed class FileDocumentStore : IDocumentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _basePath;

    public FileDocumentStore(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path is required.", nameof(basePath));
        }

        _basePath = basePath;
    }

    public async Task<T?> GetAsync<T>(
        string collection,
        string id,
        CancellationToken ct) where T : class
    {
        ValidateArgs(collection, id);

        var filePath = GetFilePath(collection, id);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync<T>(
        string collection,
        string id,
        T document,
        CancellationToken ct) where T : class
    {
        ValidateArgs(collection, id);
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var directoryPath = GetCollectionPath(collection);
        Directory.CreateDirectory(directoryPath);

        var filePath = GetFilePath(collection, id);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, ct)
            .ConfigureAwait(false);
    }

    public Task DeleteAsync(
        string collection,
        string id,
        CancellationToken ct)
    {
        ValidateArgs(collection, id);

        var filePath = GetFilePath(collection, id);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<T> QueryAsync<T>(
        string collection,
        [EnumeratorCancellation] CancellationToken ct) where T : class
    {
        if (string.IsNullOrEmpty(collection))
        {
            throw new ArgumentException("Collection is required.", nameof(collection));
        }

        var directoryPath = GetCollectionPath(collection);
        if (!Directory.Exists(directoryPath))
        {
            yield break;
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.json"))
        {
            ct.ThrowIfCancellationRequested();

            await using var stream = File.OpenRead(filePath);
            var doc = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct)
                .ConfigureAwait(false);

            if (doc is not null)
            {
                yield return doc;
            }
        }
    }

    private string GetCollectionPath(string collection) =>
        Path.Combine(_basePath, SanitizeName(collection));

    private string GetFilePath(string collection, string id) =>
        Path.Combine(_basePath, SanitizeName(collection), $"{SanitizeName(id)}.json");

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Create(name.Length, (name, invalid), static (span, state) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = Array.IndexOf(state.invalid, state.name[i]) >= 0 ? '_' : state.name[i];
            }
        });

        return sanitized;
    }

    private static void ValidateArgs(string collection, string id)
    {
        if (string.IsNullOrEmpty(collection))
        {
            throw new ArgumentException("Collection is required.", nameof(collection));
        }

        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }
    }
}
