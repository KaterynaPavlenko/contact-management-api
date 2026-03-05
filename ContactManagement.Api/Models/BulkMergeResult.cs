namespace ContactManagement.Api.Models;

public record BulkMergeResult(
    int CreatedCount,
    int UpdatedCount,
    int DeletedCount,
    IReadOnlyList<int> CreatedIds,
    IReadOnlyList<int> UpdatedIds,
    IReadOnlyList<int> DeletedIds
);
