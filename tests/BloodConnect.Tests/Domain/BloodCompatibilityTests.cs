using BloodConnect.Domain;
using Xunit;

namespace BloodConnect.Tests.Domain;

public class BloodCompatibilityTests
{
    [Theory]
    // O- is the universal red-cell donor: compatible with every recipient group.
    [InlineData(BloodGroup.ONegative, BloodGroup.ONegative, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.OPositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.APositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.ANegative, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.BPositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.BNegative, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.ABNegative, true)]
    // AB+ is the universal red-cell recipient: accepts every donor group.
    [InlineData(BloodGroup.OPositive, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.ANegative, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.APositive, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.BNegative, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.BPositive, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.ABNegative, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.ABPositive, true)]
    // Negative recipients can only receive from negative donors of a compatible ABO group.
    [InlineData(BloodGroup.OPositive, BloodGroup.ONegative, false)]
    [InlineData(BloodGroup.APositive, BloodGroup.ANegative, false)]
    [InlineData(BloodGroup.BPositive, BloodGroup.BNegative, false)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.ABNegative, false)]
    // Mismatched ABO groups are never compatible regardless of Rh factor.
    [InlineData(BloodGroup.APositive, BloodGroup.BPositive, false)]
    [InlineData(BloodGroup.BPositive, BloodGroup.APositive, false)]
    [InlineData(BloodGroup.APositive, BloodGroup.OPositive, false)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.OPositive, false)]
    public void IsCompatible_MatchesStandardAboRhRules(BloodGroup donor, BloodGroup recipient, bool expected)
    {
        Assert.Equal(expected, BloodCompatibility.IsCompatible(donor, recipient));
    }

    [Theory]
    [InlineData(BloodGroup.ONegative, 1)]
    [InlineData(BloodGroup.OPositive, 2)]
    [InlineData(BloodGroup.ANegative, 2)]
    [InlineData(BloodGroup.APositive, 4)]
    [InlineData(BloodGroup.BNegative, 2)]
    [InlineData(BloodGroup.BPositive, 4)]
    [InlineData(BloodGroup.ABNegative, 4)]
    [InlineData(BloodGroup.ABPositive, 8)]
    public void GetCompatibleDonorGroups_ReturnsExpectedCount(BloodGroup recipient, int expectedCount)
    {
        var donors = BloodCompatibility.GetCompatibleDonorGroups(recipient);
        Assert.Equal(expectedCount, donors.Count);
    }

    [Fact]
    public void GetCompatibleDonorGroups_AlwaysIncludesUniversalDonor()
    {
        foreach (BloodGroup recipient in Enum.GetValues<BloodGroup>())
        {
            Assert.Contains(BloodGroup.ONegative, BloodCompatibility.GetCompatibleDonorGroups(recipient));
        }
    }
}
