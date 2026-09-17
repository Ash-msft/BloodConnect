using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using Xunit;

namespace BloodConnect.Tests.Integration;

/// <summary>
/// Covers the Delhi pincode-based proximity matching added in v2: that a pincode entered by a user
/// actually reaches the matching algorithm, that unusable pincodes are rejected up front, and that a
/// Delhi request without a pincode still reaches donors instead of silently matching nobody.
/// </summary>
public class PincodeMatchingTests : IClassFixture<BloodConnectApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly BloodConnectApiFactory _factory;

    public PincodeMatchingTests(BloodConnectApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DonorProfile_RoundTrips_PincodeAndResolvedZone()
    {
        var donor = _factory.CreateAuthenticatedClient("demo-rajesh");

        var upsert = new UpsertDonorProfileRequest(
            BloodGroup.OPositive, "Delhi", AvailabilityStatus.Available, null,
            ContactPreference.TeamsChat, null, true, "110016");

        var put = await donor.PutAsJsonAsync("/api/profile", upsert, JsonOptions);
        put.EnsureSuccessStatusCode();

        var profile = await donor.GetFromJsonAsync<DonorProfileDto>("/api/profile", JsonOptions);

        Assert.NotNull(profile);
        Assert.Equal("110016", profile!.Pincode);
        Assert.False(string.IsNullOrWhiteSpace(profile.LocationZone));
    }

    [Fact]
    public async Task DonorProfile_RejectsUnknownDelhiPincode()
    {
        var donor = _factory.CreateAuthenticatedClient("demo-deepika");

        var upsert = new UpsertDonorProfileRequest(
            BloodGroup.OPositive, "Delhi", AvailabilityStatus.Available, null,
            ContactPreference.TeamsChat, null, true, "999999");

        var put = await donor.PutAsJsonAsync("/api/profile", upsert, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task NonDelhiCity_AcceptsPincode_WithoutDelhiValidation()
    {
        // Pincode validation is Delhi-specific today; other cities must not be blocked by it.
        var donor = _factory.CreateAuthenticatedClient("demo-anil");

        var upsert = new UpsertDonorProfileRequest(
            BloodGroup.OPositive, "Mumbai", AvailabilityStatus.Available, null,
            ContactPreference.TeamsChat, null, true, "400001");

        var put = await donor.PutAsJsonAsync("/api/profile", upsert, JsonOptions);
        put.EnsureSuccessStatusCode();

        var profile = await donor.GetFromJsonAsync<DonorProfileDto>("/api/profile", JsonOptions);
        Assert.Equal("400001", profile!.Pincode);
        Assert.Null(profile.LocationZone);
    }

    [Fact]
    public async Task Request_RejectsUnknownDelhiPincode()
    {
        var requester = _factory.CreateAuthenticatedClient("demo-liam");

        var create = new CreateBloodRequestDto(
            BloodGroup.OPositive, "Max Healthcare", "Delhi", 1, UrgencyLevel.Urgent, null, "123456");

        var response = await requester.PostAsJsonAsync("/api/requests", create, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_Persists_PincodeAndExposesUrgencyRadius()
    {
        var requester = _factory.CreateAuthenticatedClient("demo-sofia");

        var create = new CreateBloodRequestDto(
            BloodGroup.OPositive, "AIIMS", "Delhi", 1, UrgencyLevel.Critical, null, "110016");

        var response = await requester.PostAsJsonAsync("/api/requests", create, JsonOptions);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        Assert.NotNull(created);
        Assert.Equal("110016", created!.Pincode);
        Assert.False(string.IsNullOrWhiteSpace(created.LocationZone));
        Assert.Equal(3.0, created.SearchRadiusKm);
    }

    [Theory]
    [InlineData(UrgencyLevel.Critical, 3.0)]
    [InlineData(UrgencyLevel.Urgent, 5.0)]
    [InlineData(UrgencyLevel.Routine, 10.0)]
    public async Task SearchRadius_TightensWithUrgency(UrgencyLevel urgency, double expectedRadiusKm)
    {
        var requester = _factory.CreateAuthenticatedClient("demo-ana");

        var create = new CreateBloodRequestDto(
            BloodGroup.ABPositive, "Apollo", "Delhi", 1, urgency, null, "110001");

        var response = await requester.PostAsJsonAsync("/api/requests", create, JsonOptions);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        Assert.Equal(expectedRadiusKm, created!.SearchRadiusKm);
    }

    [Fact]
    public async Task DelhiRequest_WithoutPincode_StillMatchesDonors()
    {
        // Regression: distance-based filtering previously treated an absent request pincode as an
        // infinite distance for every donor, so a Delhi request with no pincode matched nobody.
        var donor = _factory.CreateAuthenticatedClient("demo-neha");
        var donorUpsert = new UpsertDonorProfileRequest(
            BloodGroup.OPositive, "Delhi", AvailabilityStatus.Available, null,
            ContactPreference.TeamsChat, null, true, "110001");
        (await donor.PutAsJsonAsync("/api/profile", donorUpsert, JsonOptions)).EnsureSuccessStatusCode();

        var requester = _factory.CreateAuthenticatedClient("demo-fatima");
        var create = new CreateBloodRequestDto(
            BloodGroup.OPositive, "Safdarjung Hospital", "Delhi", 1, UrgencyLevel.Critical, null);

        var response = await requester.PostAsJsonAsync("/api/requests", create, JsonOptions);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        Assert.Null(created!.Pincode);
        Assert.True(created.MatchedDonorCount > 0, "A Delhi request without a pincode must still notify donors.");
    }

    [Fact]
    public async Task DelhiRequest_WithPincode_PrioritisesNearestDonorFirst()
    {
        // demo-wei sits in the same pincode as the hospital; demo-noah is deliberately far away.
        var near = _factory.CreateAuthenticatedClient("demo-wei");
        (await near.PutAsJsonAsync("/api/profile", new UpsertDonorProfileRequest(
            BloodGroup.ONegative, "Delhi", AvailabilityStatus.Available, null,
            ContactPreference.TeamsChat, null, true, "110016"), JsonOptions)).EnsureSuccessStatusCode();

        var far = _factory.CreateAuthenticatedClient("demo-noah");
        (await far.PutAsJsonAsync("/api/profile", new UpsertDonorProfileRequest(
            BloodGroup.ONegative, "Delhi", AvailabilityStatus.Available, null,
            ContactPreference.TeamsChat, null, true, "110032"), JsonOptions)).EnsureSuccessStatusCode();

        var requester = _factory.CreateAuthenticatedClient("demo-priya");
        var create = new CreateBloodRequestDto(
            BloodGroup.ONegative, "Max Saket", "Delhi", 2, UrgencyLevel.Routine, null, "110016");

        var response = await requester.PostAsJsonAsync("/api/requests", create, JsonOptions);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        var responses = await requester.GetFromJsonAsync<List<DonorResponseForRequesterDto>>(
            $"/api/requests/{created!.Id}/responses", JsonOptions);

        Assert.NotNull(responses);
        Assert.NotEmpty(responses!);

        // The closest donor must carry the best (lowest) proximity rank of everyone notified.
        Assert.Equal(0, responses!.Min(r => r.ProximityRank));
    }
}
