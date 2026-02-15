namespace DrimAgents.Api.Common.Pagination;

public record PageResponse<T>(T[] Items, string? NextPageToken);
