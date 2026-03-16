using FluentAssertions;

using OpenVEPA.Storage;

namespace OpenVEPA.Storage.Tests;

public sealed class FileDocumentStoreTests : IAsyncLifetime
{
    private string _tempDir = null!;
    private FileDocumentStore _store = null!;

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"openvpa-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _store = new FileDocumentStore(_tempDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task WriteDocument_ThenReadDocument_ReturnsSameContent()
    {
        var doc = new TestDocument { Name = "Alice", Age = 30 };

        await _store.UpsertAsync("people", "alice", doc, CancellationToken.None);

        var result = await _store.GetAsync<TestDocument>("people", "alice", CancellationToken.None)
            ;

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public async Task ReadDocument_NonExistent_ReturnsNull()
    {
        var result = await _store.GetAsync<TestDocument>("people", "nobody", CancellationToken.None)
            ;

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocument_RemovesTheFile()
    {
        var doc = new TestDocument { Name = "Bob", Age = 25 };
        await _store.UpsertAsync("people", "bob", doc, CancellationToken.None);

        await _store.DeleteAsync("people", "bob", CancellationToken.None);

        var result = await _store.GetAsync<TestDocument>("people", "bob", CancellationToken.None)
            ;
        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryAsync_ReturnsAllWrittenDocs()
    {
        await _store.UpsertAsync("items", "item-1", new TestDocument { Name = "One", Age = 1 },
            CancellationToken.None);
        await _store.UpsertAsync("items", "item-2", new TestDocument { Name = "Two", Age = 2 },
            CancellationToken.None);
        await _store.UpsertAsync("items", "item-3", new TestDocument { Name = "Three", Age = 3 },
            CancellationToken.None);

        var docs = new List<TestDocument>();
        await foreach (var doc in _store.QueryAsync<TestDocument>("items", CancellationToken.None)
            )
        {
            docs.Add(doc);
        }

        docs.Should().HaveCount(3);
    }

    [Fact]
    public async Task QueryAsync_EmptyCollection_ReturnsEmpty()
    {
        var docs = new List<TestDocument>();
        await foreach (var doc in _store.QueryAsync<TestDocument>("empty-collection", CancellationToken.None)
            )
        {
            docs.Add(doc);
        }

        docs.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertAsync_OverwritesExistingDocument()
    {
        await _store.UpsertAsync("data", "key", new TestDocument { Name = "Original", Age = 1 },
            CancellationToken.None);

        await _store.UpsertAsync("data", "key", new TestDocument { Name = "Updated", Age = 2 },
            CancellationToken.None);

        var result = await _store.GetAsync<TestDocument>("data", "key", CancellationToken.None)
            ;

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Age.Should().Be(2);
    }

    [Fact]
    public async Task DeleteDocument_NonExistent_DoesNotThrow()
    {
        var act = () => _store.DeleteAsync("collection", "nonexistent", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    public sealed class TestDocument
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
