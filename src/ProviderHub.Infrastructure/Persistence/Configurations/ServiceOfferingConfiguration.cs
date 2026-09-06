using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Services;

namespace ProviderHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the relationship that carries its own data: which provider offers which service, and in
/// which countries.
/// </summary>
public sealed class ServiceOfferingConfiguration : IEntityTypeConfiguration<ServiceOffering>
{
    public void Configure(EntityTypeBuilder<ServiceOffering> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ServiceOfferings");

        builder.HasKey(offering => offering.Id);
        builder.Property(offering => offering.Id).ValueGeneratedOnAdd();

        builder.Property(offering => offering.ProviderId).IsRequired();
        builder.Property(offering => offering.ServiceId).IsRequired();

        // A provider offers a given service once. The aggregate enforces it in memory; this
        // index enforces it for everyone, including a second process the aggregate cannot see.
        builder.HasIndex(offering => new { offering.ProviderId, offering.ServiceId })
            .IsUnique()
            .HasDatabaseName("UX_ServiceOfferings_Provider_Service");

        // Pointing at the other aggregate by identifier, with no navigation property: an
        // aggregate references another one, it does not contain it. Deleting a catalogue entry
        // that providers still offer is refused rather than silently cascading.
        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(offering => offering.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(offering => offering.Countries, countries =>
        {
            countries.ToTable("ServiceOfferingCountries");

            countries.WithOwner().HasForeignKey("ServiceOfferingId");

            countries.Property(country => country.Value)
                .HasColumnName("CountryCode")
                .HasColumnType("char(2)")
                .IsRequired();

            // The name is resolved from the code at runtime, so it is never stored: one country,
            // one row, one spelling. That is what keeps the country indicators trustworthy.
            countries.Ignore(country => country.DisplayName);

            countries.HasKey("ServiceOfferingId", nameof(Domain.Common.ValueObjects.CountryCode.Value));
        });

        builder.Metadata
            .FindNavigation(nameof(ServiceOffering.Countries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(offering => offering.IsTransient);
    }
}
