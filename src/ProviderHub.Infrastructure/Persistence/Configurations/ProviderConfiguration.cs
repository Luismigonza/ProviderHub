using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Provider"/> to the <c>Providers</c> table.
/// <para>
/// Value objects are stored as owned types rather than through a value converter, so that their
/// parts stay real columns: a converter would turn <c>Nit</c> into an opaque string that no
/// query could look inside, and searching by tax identifier is a requirement.
/// </para>
/// </summary>
public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Providers");

        builder.HasKey(provider => provider.Id);
        builder.Property(provider => provider.Id).ValueGeneratedOnAdd();

        builder.Property(provider => provider.Name)
            .HasMaxLength(Provider.NameMaxLength)
            .IsRequired();

        builder.OwnsOne(provider => provider.Nit, nit =>
        {
            nit.Property(value => value.BaseNumber)
                .HasColumnName("NitBaseNumber")
                .HasMaxLength(15)
                .IsRequired();

            nit.Property(value => value.CheckDigit)
                .HasColumnName("NitCheckDigit")
                .HasColumnType("tinyint")
                .IsRequired();

            // The canonical text is derived from the two columns above; storing it as well would
            // be a third copy of the same fact, and one more thing that can fall out of sync.
            nit.Ignore(value => value.Value);

            // No two providers can share a tax identifier. The use case checks this before
            // saving, but only the database can win the race between two simultaneous requests.
            nit.HasIndex(value => value.BaseNumber).IsUnique().HasDatabaseName("UX_Providers_Nit");
        });

        builder.Navigation(provider => provider.Nit).IsRequired();

        builder.OwnsOne(provider => provider.Website, website =>
            website.Property(value => value.Value)
                .HasColumnName("Website")
                .HasMaxLength(WebsiteUrl.MaxLength)
                .IsRequired());

        builder.Navigation(provider => provider.Website).IsRequired();

        builder.OwnsOne(provider => provider.Email, email =>
        {
            email.Property(value => value.Value)
                .HasColumnName("Email")
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();

            email.HasIndex(value => value.Value).HasDatabaseName("IX_Providers_Email");
        });

        builder.Navigation(provider => provider.Email).IsRequired();

        builder.HasMany(provider => provider.Offerings)
            .WithOne()
            .HasForeignKey(offering => offering.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // The aggregate exposes a read-only view of its offerings, so Entity Framework is told
        // to go through the backing field. Encapsulation survives persistence.
        builder.Metadata
            .FindNavigation(nameof(Provider.Offerings))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(provider => provider.DomainEvents);
        builder.Ignore(provider => provider.IsTransient);
    }
}
