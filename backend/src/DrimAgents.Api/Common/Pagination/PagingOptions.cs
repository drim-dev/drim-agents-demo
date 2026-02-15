namespace DrimAgents.Api.Common.Pagination;

public class PagingOptions
{
    public string TokenEncryptionKeyInBase64 { get; set; } = string.Empty;

    public string TokenIvInBase64 { get; set; } = string.Empty;

    public int DefaultMaxPageSize { get; set; } = 10;

    public int MaxMaxPageSize { get; set; } = 100;
}
