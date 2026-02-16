namespace DrimAgents.Api.Common.Options;

public class EncryptionOptions
{
    public string PaginationKey { get; set; } = string.Empty;
    public string DataProtectionKey { get; set; } = string.Empty;
}
