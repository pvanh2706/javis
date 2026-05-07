using JavisApi.Data;
using JavisApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JavisApi.Services;

/// <summary>
/// Encrypts/decrypts AI provider API keys stored in the app_config table.
/// Uses AES-256 with a key derived from the app's JWT secret.
/// </summary>
public class ConfigService
{
    private readonly AppDbContext _db;
    private readonly byte[] _encryptionKey;

    public ConfigService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        // Derive a 32-byte key from JWT secret
        var secret = config["Jwt:SecretKey"] ?? "default-secret";
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public async Task<string?> GetAsync(string key)
    {
        var cfg = await _db.AppConfigs.FindAsync(key);
        return cfg?.Value is null ? null : Decrypt(cfg.Value);
    }

    public async Task SetAsync(string key, string value)
    {
        var encrypted = Encrypt(value);
        var cfg = await _db.AppConfigs.FindAsync(key);
        if (cfg is null)
        {
            _db.AppConfigs.Add(new AppConfig { Key = key, Value = encrypted });
        }
        else
        {
            cfg.Value = encrypted;
            cfg.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string?>> GetAllAsync()
    {
        var configs = await _db.AppConfigs.ToListAsync();
        return configs.ToDictionary(c => c.Key, c => c.Value is null ? null : TryDecrypt(c.Value));
    }

    private string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + ciphertext.Length];
        aes.IV.CopyTo(result, 0);
        ciphertext.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string ciphertext)
    {
        var data = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var iv = data[..16];
        var cipher = data[16..];
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private string? TryDecrypt(string value)
    {
        try { return Decrypt(value); }
        catch { return null; }
    }
}
