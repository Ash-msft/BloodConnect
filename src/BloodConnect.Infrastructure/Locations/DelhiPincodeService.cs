using System.Collections.Generic;
using System.Linq;

namespace BloodConnect.Infrastructure.Locations;

/// <summary>
/// Delhi pincode reference service for proximity-based donor matching.
/// Maps pincodes to Delhi zones and calculates distance between pincodes.
/// </summary>
public static class DelhiPincodeService
{
    /// <summary>
    /// Delhi pincodes organized by zone with approximate coordinates.
    /// Format: Pincode -> (Zone Name, Latitude, Longitude)
    /// </summary>
    private static readonly Dictionary<string, (string Zone, double Lat, double Lng)> DelhiPincodes = new()
    {
        // Central Delhi
        ["110001"] = ("Central - New Delhi", 28.6139, 77.2090),
        ["110002"] = ("Central - New Delhi", 28.6139, 77.2090),
        ["110003"] = ("Central - New Delhi", 28.6139, 77.2090),
        ["110004"] = ("Central - New Delhi", 28.6139, 77.2090),
        ["110005"] = ("Central - New Delhi", 28.6139, 77.2090),
        ["110006"] = ("Central - New Delhi", 28.6139, 77.2090),

        // North Delhi
        ["110007"] = ("North - Kasturba Nagar", 28.6500, 77.2400),
        ["110008"] = ("North - North Delhi", 28.6500, 77.2400),
        ["110009"] = ("North - Karol Bagh", 28.6500, 77.2100),
        ["110010"] = ("North - Chandni Chowk", 28.6505, 77.2303),

        // South Delhi
        ["110011"] = ("South - South Delhi", 28.5244, 77.1855),
        ["110012"] = ("South - South Delhi", 28.5244, 77.1855),
        ["110013"] = ("South - Sector 13", 28.5244, 77.1855),
        ["110014"] = ("South - Greater Kailash", 28.5216, 77.2019),
        ["110015"] = ("South - Hauz Khas", 28.5244, 77.1855),
        ["110016"] = ("South - South Delhi", 28.5244, 77.1855),

        // East Delhi
        ["110017"] = ("East - East Delhi", 28.5921, 77.3084),
        ["110018"] = ("East - Laxmi Nagar", 28.5921, 77.3084),
        ["110019"] = ("East - Preet Vihar", 28.5921, 77.3084),
        ["110020"] = ("East - East Delhi", 28.5921, 77.3084),

        // West Delhi
        ["110021"] = ("West - West Delhi", 28.6367, 77.0488),
        ["110022"] = ("West - West Delhi", 28.6367, 77.0488),
        ["110023"] = ("West - Vikaspuri", 28.5900, 77.0700),
        ["110024"] = ("West - Kirti Nagar", 28.5900, 77.0700),

        // Northeast Delhi
        ["110025"] = ("Northeast - Northeast Delhi", 28.7041, 77.3110),
        ["110026"] = ("Northeast - Shalimar Bagh", 28.7500, 77.2000),
        ["110027"] = ("Northeast - Rani Garden", 28.7500, 77.2000),
        ["110028"] = ("Northeast - Bhajanpura", 28.7041, 77.3110),

        // Northwest Delhi
        ["110029"] = ("Northwest - Rohini", 28.7497, 77.0554),
        ["110030"] = ("Northwest - Rohini", 28.7497, 77.0554),
        ["110031"] = ("Northwest - Narela", 28.8241, 77.2555),
        ["110032"] = ("Northwest - Pitampura", 28.7373, 77.1147),

        // Southeast Delhi
        ["110033"] = ("Southeast - Kalkaji", 28.5177, 77.2504),
        ["110034"] = ("Southeast - Kalkaji", 28.5177, 77.2504),
        ["110035"] = ("Southeast - Southeast Delhi", 28.5400, 77.2600),
        ["110036"] = ("Southeast - Sangam Vihar", 28.5400, 77.2600),

        // Southwest Delhi
        ["110037"] = ("Southwest - Chhatarpur", 28.4744, 77.1762),
        ["110038"] = ("Southwest - Chhatarpur", 28.4744, 77.1762),
        ["110039"] = ("Southwest - Neb Sarai", 28.4744, 77.1762),
        ["110040"] = ("Southwest - Mehrauli", 28.5244, 77.1745),

        // New Delhi zones
        ["110041"] = ("New Delhi - Parliament Street", 28.6139, 77.2090),
        ["110042"] = ("New Delhi - Lutyens Delhi", 28.6139, 77.2090),
        ["110043"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110044"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110045"] = ("New Delhi - Connaught Place", 28.6328, 77.2197),
        ["110046"] = ("New Delhi - Pusa", 28.6067, 77.1617),
        ["110047"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110048"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110049"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110050"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110051"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110052"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110053"] = ("New Delhi - Safdarjung", 28.5732, 77.1921),
        ["110054"] = ("New Delhi - Safdarjung", 28.5732, 77.1921),
        ["110055"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110056"] = ("New Delhi - Green Park", 28.5617, 77.1967),
        ["110057"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110058"] = ("New Delhi - Panchsheel", 28.5244, 77.1855),
        ["110059"] = ("New Delhi - Defence Colony", 28.5633, 77.2435),
        ["110060"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110061"] = ("New Delhi - Jorabagh", 28.6016, 77.2218),
        ["110062"] = ("New Delhi - East End Road", 28.6048, 77.1973),
        ["110063"] = ("New Delhi - Jor Bagh", 28.6016, 77.2218),
        ["110064"] = ("New Delhi - Vasant Kunj", 28.5244, 77.1855),
        ["110065"] = ("New Delhi - Vasant Kunj", 28.5244, 77.1855),
        ["110066"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110067"] = ("New Delhi - Vasant Vihar", 28.5244, 77.1855),
        ["110068"] = ("New Delhi - Vasant Vihar", 28.5244, 77.1855),
        ["110069"] = ("New Delhi - New Delhi", 28.6139, 77.2090),
        ["110070"] = ("New Delhi - Chanakyapuri", 28.5732, 77.1921),
        ["110071"] = ("New Delhi - Chanakyapuri", 28.5732, 77.1921),
    };

    /// <summary>
    /// Calculates distance in kilometers between two pincodes using Haversine formula.
    /// Returns null if either pincode is not found in Delhi database.
    /// </summary>
    public static double? CalculateDistanceKm(string? fromPincode, string? toPincode)
    {
        if (string.IsNullOrWhiteSpace(fromPincode) || string.IsNullOrWhiteSpace(toPincode))
            return null;

        if (!DelhiPincodes.TryGetValue(fromPincode, out var fromLocation) ||
            !DelhiPincodes.TryGetValue(toPincode, out var toLocation))
            return null;

        return HaversineDistance(fromLocation.Lat, fromLocation.Lng, toLocation.Lat, toLocation.Lng);
    }

    /// <summary>
    /// Returns true if the given pincode is a valid Delhi pincode.
    /// </summary>
    public static bool IsValidDelhiPincode(string? pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode))
            return false;

        return DelhiPincodes.ContainsKey(pincode);
    }

    /// <summary>
    /// Gets the zone name for a Delhi pincode, or null if not found.
    /// </summary>
    public static string? GetZoneName(string? pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode))
            return null;

        return DelhiPincodes.TryGetValue(pincode, out var location) ? location.Zone : null;
    }

    /// <summary>
    /// Returns all valid Delhi pincodes.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllValidPincodes()
    {
        return DelhiPincodes.Keys;
    }

    /// <summary>
    /// Haversine formula to calculate distance between two coordinates in km.
    /// </summary>
    private static double HaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth's radius in kilometers
        const double DegToRad = Math.PI / 180;

        var dLat = (lat2 - lat1) * DegToRad;
        var dLng = (lng2 - lng1) * DegToRad;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * DegToRad) * Math.Cos(lat2 * DegToRad) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));
        return R * c;
    }
}
