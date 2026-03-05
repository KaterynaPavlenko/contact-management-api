namespace ContactManagement.Api.Models;

public record BulkMergeRequest(IReadOnlyList<BulkMergeContactItem> Items);
