using ProviderHub.Domain.Common;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Domain.Tests.Providers;

public class NitTests
{
    [Theory]
    [InlineData("890903938-8")]
    [InlineData("900373115-3")]
    [InlineData("811021363-0")]
    public void A_nit_with_a_correct_check_digit_is_accepted(string value)
    {
        Assert.Equal(value, Nit.Create(value).Value);
    }

    [Theory]
    [InlineData("890.903.938-8")]
    [InlineData("890 903 938 - 8")]
    [InlineData("  890903938-8  ")]
    public void The_usual_written_formats_are_all_understood(string value)
    {
        // Whichever way it was typed, it is stored once, in one canonical shape.
        Assert.Equal("890903938-8", Nit.Create(value).Value);
    }

    [Fact]
    public void A_wrong_check_digit_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(() => Nit.Create("890903938-1"));

        Assert.Contains("check digit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("890903938")]
    [InlineData("8909-3")]
    [InlineData("89090393A-8")]
    [InlineData("890903938-11")]
    public void A_malformed_nit_is_rejected(string? value)
    {
        Assert.Throws<DomainException>(() => Nit.Create(value));
    }

    [Fact]
    public void The_base_number_and_the_check_digit_are_kept_apart()
    {
        var nit = Nit.Create("890903938-8");

        Assert.Equal("890903938", nit.BaseNumber);
        Assert.Equal(8, nit.CheckDigit);
    }

    [Theory]
    [InlineData("890903938", 8)]
    [InlineData("900373115", 3)]
    [InlineData("811021363", 0)]
    public void The_check_digit_is_derived_from_the_official_algorithm(string baseNumber, int expected)
    {
        Assert.Equal(expected, Nit.CalculateCheckDigit(baseNumber));
    }
}
