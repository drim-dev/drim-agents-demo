using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SimpleBase;

namespace DrimAgents.Api.Common.Pagination;

public class LimitOffsetPaging
{
    private readonly PagingOptions _options;
    private readonly byte[] _encryptionKey;
    private readonly byte[] _iv;

    public LimitOffsetPaging(IOptions<PagingOptions> options)
    {
        _options = options.Value;
        _encryptionKey = Convert.FromBase64String(_options.TokenEncryptionKeyInBase64);
        _iv = Convert.FromBase64String(_options.TokenIvInBase64);

        if (_encryptionKey.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits)");
        if (_iv.Length != 16)
            throw new InvalidOperationException("IV must be 16 bytes (128 bits)");
    }

    public bool TryGetMaxPageSize(int? requestedMaxPageSize, out int maxPageSize)
    {
        if (requestedMaxPageSize is null)
        {
            maxPageSize = _options.DefaultMaxPageSize;
            return true;
        }

        if (requestedMaxPageSize.Value <= 0 || requestedMaxPageSize.Value > _options.MaxMaxPageSize)
        {
            maxPageSize = 0;
            return false;
        }

        maxPageSize = requestedMaxPageSize.Value;
        return true;
    }

    public bool TryGetOffsetAndLimit(
        string? pageToken,
        int maxPageSize,
        out int? offset,
        out int? limit,
        params object?[] queryParams)
    {
        limit = maxPageSize;

        if (string.IsNullOrEmpty(pageToken))
        {
            offset = 0;
            return true;
        }

        try
        {
            var tokenData = DecryptToken(pageToken);
            var currentHash = ComputeQueryHash(queryParams);

            if (tokenData.QueryHash != currentHash)
            {
                offset = null;
                return false;
            }

            offset = tokenData.Offset;
            return true;
        }
        catch
        {
            offset = null;
            return false;
        }
    }

    public string? CreateNextPageToken(
        int itemCount,
        int currentOffset,
        int limit,
        params object?[] queryParams)
    {
        if (itemCount < limit)
        {
            return null;
        }

        var nextOffset = currentOffset + limit;
        var queryHash = ComputeQueryHash(queryParams);
        var tokenData = new PageTokenData(nextOffset, queryHash);

        return EncryptToken(tokenData);
    }

    private string ComputeQueryHash(params object?[] queryParams)
    {
        var json = JsonSerializer.Serialize(queryParams);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private string EncryptToken(PageTokenData data)
    {
        var json = JsonSerializer.Serialize(data);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Base32.Crockford.Encode(encrypted);
    }

    private PageTokenData DecryptToken(string encryptedToken)
    {
        var encrypted = Base32.Crockford.Decode(encryptedToken);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        var json = Encoding.UTF8.GetString(decrypted);

        return JsonSerializer.Deserialize<PageTokenData>(json)
            ?? throw new InvalidOperationException("Failed to deserialize page token");
    }

    private record PageTokenData(int Offset, string QueryHash);
}
