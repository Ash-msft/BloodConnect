namespace BloodConnect.Domain;

/// <summary>
/// Encodes standard red-cell (whole blood/packed red cell) ABO/Rh donor compatibility rules.
/// This is informational only: see the medical disclaimer surfaced throughout the app and README.
/// Real-world compatibility decisions must always be confirmed by a licensed donation center.
/// </summary>
public static class BloodCompatibility
{
    // Maps each recipient blood group to the set of blood groups that may donate red cells to them.
    private static readonly Dictionary<BloodGroup, BloodGroup[]> CompatibleDonorsByRecipient = new()
    {
        [BloodGroup.ONegative] = new[] { BloodGroup.ONegative },
        [BloodGroup.OPositive] = new[] { BloodGroup.ONegative, BloodGroup.OPositive },
        [BloodGroup.ANegative] = new[] { BloodGroup.ONegative, BloodGroup.ANegative },
        [BloodGroup.APositive] = new[]
        {
            BloodGroup.ONegative, BloodGroup.OPositive, BloodGroup.ANegative, BloodGroup.APositive
        },
        [BloodGroup.BNegative] = new[] { BloodGroup.ONegative, BloodGroup.BNegative },
        [BloodGroup.BPositive] = new[]
        {
            BloodGroup.ONegative, BloodGroup.OPositive, BloodGroup.BNegative, BloodGroup.BPositive
        },
        [BloodGroup.ABNegative] = new[]
        {
            BloodGroup.ONegative, BloodGroup.ANegative, BloodGroup.BNegative, BloodGroup.ABNegative
        },
        [BloodGroup.ABPositive] = new[]
        {
            BloodGroup.ONegative, BloodGroup.OPositive, BloodGroup.ANegative, BloodGroup.APositive,
            BloodGroup.BNegative, BloodGroup.BPositive, BloodGroup.ABNegative, BloodGroup.ABPositive
        }
    };

    /// <summary>
    /// Returns true if a donor with <paramref name="donor"/> blood group can safely donate red cells
    /// to a recipient with <paramref name="recipient"/> blood group, per standard ABO/Rh rules.
    /// </summary>
    public static bool IsCompatible(BloodGroup donor, BloodGroup recipient)
        => CompatibleDonorsByRecipient[recipient].Contains(donor);

    /// <summary>
    /// Returns every donor blood group compatible with the given recipient blood group.
    /// </summary>
    public static IReadOnlyCollection<BloodGroup> GetCompatibleDonorGroups(BloodGroup recipient)
        => CompatibleDonorsByRecipient[recipient];
}
