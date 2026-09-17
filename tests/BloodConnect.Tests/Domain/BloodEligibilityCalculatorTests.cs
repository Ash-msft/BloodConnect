using BloodConnect.Domain;
using Xunit;

namespace BloodConnect.Tests.Domain;

public class BloodEligibilityCalculatorTests
{
    [Fact]
    public void IsEligible_NeverDonatedBefore_IsAlwaysEligible()
    {
        Assert.True(BloodEligibilityCalculator.IsEligible(null, DateTime.UtcNow));
    }

    [Fact]
    public void IsEligible_ExactlyAtBoundary_IsEligible()
    {
        var lastDonation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var asOf = lastDonation.AddDays(56);

        Assert.True(BloodEligibilityCalculator.IsEligible(lastDonation, asOf, 56));
    }

    [Fact]
    public void IsEligible_OneSecondBeforeBoundary_IsNotEligible()
    {
        var lastDonation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var asOf = lastDonation.AddDays(56).AddSeconds(-1);

        Assert.False(BloodEligibilityCalculator.IsEligible(lastDonation, asOf, 56));
    }

    [Fact]
    public void IsEligible_OneSecondAfterBoundary_IsEligible()
    {
        var lastDonation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var asOf = lastDonation.AddDays(56).AddSeconds(1);

        Assert.True(BloodEligibilityCalculator.IsEligible(lastDonation, asOf, 56));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(120)]
    public void IsEligible_RespectsConfiguredInterval(int intervalDays)
    {
        var lastDonation = DateTime.UtcNow.AddDays(-intervalDays);
        Assert.True(BloodEligibilityCalculator.IsEligible(lastDonation, DateTime.UtcNow, intervalDays));
        Assert.False(BloodEligibilityCalculator.IsEligible(lastDonation, DateTime.UtcNow.AddSeconds(-1), intervalDays + 1));
    }

    [Fact]
    public void IsEligible_NegativeInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BloodEligibilityCalculator.IsEligible(DateTime.UtcNow, DateTime.UtcNow, -1));
    }

    [Fact]
    public void GetNextEligibleUtc_NeverDonated_ReturnsNull()
    {
        Assert.Null(BloodEligibilityCalculator.GetNextEligibleUtc(null));
    }

    [Fact]
    public void GetNextEligibleUtc_ReturnsLastDonationPlusInterval()
    {
        var lastDonation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var next = BloodEligibilityCalculator.GetNextEligibleUtc(lastDonation, 56);

        Assert.Equal(lastDonation.AddDays(56), next);
    }
}
