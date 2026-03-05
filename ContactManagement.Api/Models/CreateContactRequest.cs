namespace ContactManagement.Api.Models;

public record CreateContactRequest(string Name, string? Email, string? Phone);
