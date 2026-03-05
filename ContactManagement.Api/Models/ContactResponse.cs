namespace ContactManagement.Api.Models;

public record ContactResponse(int Id, string Name, string? Email, string? Phone, DateTime CreatedAt, DateTime UpdatedAt);
