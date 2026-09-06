using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderHub.Domain.Services;

namespace ProviderHub.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Service"/> to the <c>Services</c> table.</summary>
public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Services");

        builder.HasKey(service => service.Id);
        builder.Property(service => service.Id).ValueGeneratedOnAdd();

        builder.Property(service => service.Name)
            .HasMaxLength(Service.NameMaxLength)
            .IsRequired();

        builder.HasIndex(service => service.Name)
            .IsUnique()
            .HasDatabaseName("UX_Services_Name");

        builder.OwnsOne(service => service.HourlyRate, rate =>
        {
            // decimal(18,2), never float: money in binary floating point is how a total ends up
            // one cent off and nobody can explain why.
            rate.Property(money => money.Amount)
                .HasColumnName("HourlyRateAmount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            rate.Property(money => money.Currency)
                .HasColumnName("HourlyRateCurrency")
                .HasColumnType("char(3)")
                .IsRequired();
        });

        builder.Navigation(service => service.HourlyRate).IsRequired();

        builder.Ignore(service => service.DomainEvents);
        builder.Ignore(service => service.IsTransient);
    }
}
