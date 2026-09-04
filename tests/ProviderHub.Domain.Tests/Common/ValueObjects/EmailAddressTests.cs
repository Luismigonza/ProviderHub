using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Tests.Common.ValueObjects;

public class EmailAddressTests
{
    [Fact]
    public void A_valid_address_is_normalized_to_lower_case()
    {
        var email = EmailAddress.Create("  Contact@Tekus.CO  ");

        Assert.Equal("contact@tekus.co", email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-address")]
    [InlineData("missing@domain@twice.com")]
    [InlineData("Tekus Support <support@tekus.co>")]
    public void An_invalid_address_is_rejected(string? value)
    {
        Assert.Throws<DomainException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void Addresses_differing_only_in_case_are_the_same_address()
    {
        Assert.Equal(EmailAddress.Create("a@tekus.co"), EmailAddress.Create("A@TEKUS.CO"));
    }
}
