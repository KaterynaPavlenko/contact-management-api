namespace ContactManagement.Api.Models;

public class ContactListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
    public string? Email { get; set; }
}
