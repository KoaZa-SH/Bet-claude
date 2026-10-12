using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BetClaude.Services;

namespace BetClaude.Infrastructure.Security;

/// <summary>Stores secrets (such as API keys) encrypted with Windows DPAPI, bound to the current Windows user.</summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretStore : ISecretStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BetClaude.v1");
    private static readonly Regex NamePattern = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

    private readonly string _directory;

    public DpapiSecretStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    public void Save(string name, string secret)
    {
        byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PathFor(name), encrypted);
    }

    public string? TryLoad(string name)
    {
        string path = PathFor(name);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] decrypted = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void Delete(string name)
    {
        string path = PathFor(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string PathFor(string name)
    {
        if (!NamePattern.IsMatch(name))
        {
            throw new ArgumentException("Secret name may contain only letters, digits, '_' and '-'.", nameof(name));
        }

        return Path.Combine(_directory, name + ".bin");
    }
}
