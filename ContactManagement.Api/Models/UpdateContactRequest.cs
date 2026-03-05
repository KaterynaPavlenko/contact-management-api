namespace ContactManagement.Api.Models;

public record UpdateContactRequest(string Name, string? Email, string? Phone);
