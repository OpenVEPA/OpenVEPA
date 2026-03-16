using FluentAssertions;

using OpenVEPA.Core.Preferences;

namespace OpenVEPA.Core.Tests.Preferences;

public sealed class PreferenceEntryTests
{
    [Fact]
    public void EmptyPreferences_HasNoEntries()
    {
        var prefs = new UserPreferences(new Dictionary<string, PreferenceEntry>());

        prefs.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Preferences_WithEntries_AreAccessible()
    {
        var now = DateTime.UtcNow;
        var entries = new Dictionary<string, PreferenceEntry>
        {
            ["theme"] = new("theme", "dark", "appearance", PreferenceSource.Explicit, 1.0, now),
            ["language"] = new("language", "en-US", "locale", PreferenceSource.Inferred, 0.8, now),
        };

        var prefs = new UserPreferences(entries);

        prefs.Entries.Should().HaveCount(2);
        prefs.Entries["theme"].Value.Should().Be("dark");
        prefs.Entries["theme"].Category.Should().Be("appearance");
        prefs.Entries["language"].Source.Should().Be(PreferenceSource.Inferred);
    }

    [Theory]
    [InlineData(PreferenceSource.Explicit)]
    [InlineData(PreferenceSource.Inferred)]
    [InlineData(PreferenceSource.Confirmed)]
    public void PreferenceEntry_AllSourceValues_CanBeCreated(PreferenceSource source)
    {
        var entry = new PreferenceEntry("key", "value", "category", source, 0.9, DateTime.UtcNow);

        entry.Source.Should().Be(source);
    }

    [Fact]
    public void PreferenceEntry_RetainsAllProperties()
    {
        var updatedAt = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var entry = new PreferenceEntry(
            Key: "food.dietary",
            Value: "vegetarian",
            Category: "food",
            Source: PreferenceSource.Confirmed,
            Confidence: 0.95,
            UpdatedAt: updatedAt);

        entry.Key.Should().Be("food.dietary");
        entry.Value.Should().Be("vegetarian");
        entry.Category.Should().Be("food");
        entry.Source.Should().Be(PreferenceSource.Confirmed);
        entry.Confidence.Should().Be(0.95);
        entry.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void PreferenceEntry_RecordEquality_SameValues_AreEqual()
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new PreferenceEntry("key", "val", "cat", PreferenceSource.Explicit, 1.0, now);
        var b = new PreferenceEntry("key", "val", "cat", PreferenceSource.Explicit, 1.0, now);

        a.Should().Be(b);
    }

    [Fact]
    public void UserPreferences_LookupByKey_ReturnsCorrectEntry()
    {
        var entries = new Dictionary<string, PreferenceEntry>
        {
            ["timezone"] = new("timezone", "UTC", "locale", PreferenceSource.Explicit, 1.0, DateTime.UtcNow),
        };

        var prefs = new UserPreferences(entries);

        prefs.Entries.ContainsKey("timezone").Should().BeTrue();
        prefs.Entries["timezone"].Value.Should().Be("UTC");
    }
}
