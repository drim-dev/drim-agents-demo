using DrimAgents.Api.Common.Exceptions;

namespace DrimAgents.Api.Common.Pagination;

public static class PaginationExceptions
{
    public static ValidationException InvalidMaxPageSize() =>
        new(new Dictionary<string, string[]>
        {
            ["maxPageSize"] = ["Invalid page size. Must be between 1 and the configured maximum."]
        });

    public static ValidationException InvalidPageToken() =>
        new(new Dictionary<string, string[]>
        {
            ["pageToken"] = ["Invalid page token. The token may be malformed, expired, or used with different query parameters."]
        });
}
