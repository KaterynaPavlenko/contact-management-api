using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ContactManagement.Api.Entities;

namespace ContactManagement.Api.Data.Configurations;

public class ContactCustomFieldValueConfiguration : IEntityTypeConfiguration<ContactCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<ContactCustomFieldValue> builder)
    {
        builder.HasKey(x => new { x.ContactId, x.CustomFieldDefinitionId });
        builder.HasOne(x => x.Contact)
            .WithMany(c => c.CustomFieldValues)
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.CustomFieldDefinition)
            .WithMany(f => f.ContactValues)
            .HasForeignKey(x => x.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ContactId, x.CustomFieldDefinitionId });
    }
}
