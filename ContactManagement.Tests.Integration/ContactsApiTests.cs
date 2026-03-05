using System.Net;
using System.Net.Http.Json;
using ContactManagement.Api.Models;
using Xunit;

namespace ContactManagement.Tests.Integration;

/// <summary>
/// Інтеграційні тести API контактів.
/// Стиль AAA (Arrange–Act–Assert) з явними коментарями.
/// Покриття: Create, GetById, GetList (пагінація, фільтр), Update, Delete, BulkMerge.
/// </summary>
public class ContactsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContactsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidRequest_Returns201AndContactWithId()
    {
        // Arrange
        var request = new CreateContactRequest("Alice", "alice@example.com", "+123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/contacts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Alice", created.Name);
        Assert.Equal("alice@example.com", created.Email);
        Assert.Equal("+123", created.Phone);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsSameContact()
    {
        // Arrange
        var request = new CreateContactRequest("Alice", "alice@example.com", "+123");
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", request);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);

        // Act
        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Name, fetched.Name);
        Assert.Equal(created.Email, fetched.Email);
        Assert.Equal(created.Phone, fetched.Phone);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingId_Returns200AndContact()
    {
        // Arrange
        var createRequest = new CreateContactRequest("Bob", "bob@test.com", null);
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);

        // Act
        var response = await _client.GetAsync($"/api/contacts/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contact = await response.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(contact);
        Assert.Equal(created.Id, contact.Id);
        Assert.Equal("Bob", contact.Name);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        // Arrange
        const int nonExistentId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/contacts/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GetList (pagination, sort, filter) ---

    [Fact]
    public async Task GetList_WithPagination_Returns200AndPagedResult()
    {
        // Arrange
        const int page = 1;
        const int pageSize = 5;

        // Act
        var response = await _client.GetAsync($"/api/contacts?page={page}&pageSize={pageSize}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ContactResponse>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.True(result.TotalCount >= 0);
        Assert.True(result.Items.Count <= pageSize);
    }

    [Fact]
    public async Task GetList_WithEmailFilter_ReturnsOnlyMatchingContacts()
    {
        // Arrange: створюємо контакт з унікальним email
        var uniqueEmail = $"filter-{Guid.NewGuid():N}@test.com";
        var createRequest = new CreateContactRequest("Filtered", uniqueEmail, null);
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act
        var response = await _client.GetAsync($"/api/contacts?email={Uri.EscapeDataString(uniqueEmail)}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ContactResponse>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        var found = result.Items.FirstOrDefault(c => c.Email == uniqueEmail);
        Assert.NotNull(found);
        Assert.Equal("Filtered", found.Name);
    }

    // --- Update ---

    [Fact]
    public async Task Update_ExistingContact_Returns200AndUpdatedData()
    {
        // Arrange
        var createRequest = new CreateContactRequest("Original", "update@test.com", null);
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);

        var updateRequest = new UpdateContactRequest("Updated Name", "update@test.com", "+999");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/contacts/{created.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("+999", updated.Phone);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        // Arrange
        const int nonExistentId = 88888;
        var updateRequest = new UpdateContactRequest("Any", "any@x.com", null);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/contacts/{nonExistentId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingContact_Returns204()
    {
        // Arrange
        var createRequest = new CreateContactRequest("ToDelete", "delete@test.com", null);
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);

        // Act
        var response = await _client.DeleteAsync($"/api/contacts/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingContact_ThenGetById_Returns404()
    {
        // Arrange
        var createRequest = new CreateContactRequest("ToDelete", "delete2@test.com", null);
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);
        await _client.DeleteAsync($"/api/contacts/{created.Id}");

        // Act
        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        // Arrange
        const int nonExistentId = 77777;

        // Act
        var response = await _client.DeleteAsync($"/api/contacts/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- BulkMerge ---

    [Fact]
    public async Task BulkMerge_NewAndExistingEmails_CreatesNewAndUpdatesExisting()
    {
        // Arrange: один контакт вже існує
        var existingRequest = new CreateContactRequest("PreExisting", "existing@merge.com", "111");
        var existingCreateResponse = await _client.PostAsJsonAsync("/api/contacts", existingRequest);
        existingCreateResponse.EnsureSuccessStatusCode();

        var bulkRequest = new BulkMergeRequest(new[]
        {
            new BulkMergeContactItem("New One", "new@merge.com", "222"),
            new BulkMergeContactItem("Updated Existing", "existing@merge.com", "111-updated")
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/contacts/bulk-merge", bulkRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BulkMergeResult>();
        Assert.NotNull(result);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Single(result.CreatedIds);
        Assert.Single(result.UpdatedIds);

        var getNewResponse = await _client.GetAsync($"/api/contacts/{result.CreatedIds.Single()}");
        getNewResponse.EnsureSuccessStatusCode();
        var newContact = await getNewResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(newContact);
        Assert.Equal("New One", newContact.Name);
        Assert.Equal("new@merge.com", newContact.Email);

        var getUpdatedResponse = await _client.GetAsync($"/api/contacts/{result.UpdatedIds.Single()}");
        getUpdatedResponse.EnsureSuccessStatusCode();
        var updatedContact = await getUpdatedResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(updatedContact);
        Assert.Equal("Updated Existing", updatedContact.Name);
        Assert.Equal("existing@merge.com", updatedContact.Email);
        Assert.Equal("111-updated", updatedContact.Phone);
    }

    [Fact]
    public async Task BulkMerge_DuplicateEmailsInRequest_Returns400()
    {
        // Arrange
        var bulkRequest = new BulkMergeRequest(new[]
        {
            new BulkMergeContactItem("A", "dup@x.com", null),
            new BulkMergeContactItem("B", "dup@x.com", null)
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/contacts/bulk-merge", bulkRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
