using System.Runtime.Versioning;
using BetClaude.Infrastructure.Security;

namespace BetClaude.Infrastructure.Tests;

[SupportedOSPlatform("windows")]
public sealed class DpapiSecretStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"betclaude-secrets-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Round_trip_and_the_file_never_contains_the_plain_text()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // DPAPI exists only on Windows; CI runs on windows-latest.
        }

        var store = new DpapiSecretStore(_dir);
        store.Save("api-football", "MY-SECRET-KEY-123");

        Assert.Equal("MY-SECRET-KEY-123", store.TryLoad("api-football"));

        string stored = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(_dir, "api-football.bin")));
        Assert.DoesNotContain("MY-SECRET-KEY-123", stored);

        store.Delete("api-football");
        Assert.Null(store.TryLoad("api-football"));
    }

    [Fact]
    public void Names_with_path_characters_are_rejected()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new DpapiSecretStore(_dir);

        Assert.Throws<ArgumentException>(() => store.Save("..\\evil", "x"));
        Assert.Throws<ArgumentException>(() => store.TryLoad("a/b"));
    }
}
