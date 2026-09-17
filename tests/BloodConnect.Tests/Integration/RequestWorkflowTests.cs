using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using Xunit;

namespace BloodConnect.Tests.Integration;

/// <summary>
/// End-to-end tests over the real HTTP pipeline: authentication, ownership checks, donor privacy,
/// and the create -> match -> respond -> reveal workflow.
/// </summary>
public class RequestWorkflowTests : IClassFixture<BloodConnectApiFactory>
{
    // The API serializes enums as strings (JsonStringEnumConverter, configured in Program.cs);
    // the test HTTP client must use the same convention when deserializing responses.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly BloodConnectApiFactory _factory;

    public RequestWorkflowTests(BloodConnectApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownDemoIdentity_IsRejected()
    {
        var client = _factory.CreateAuthenticatedClient("not-a-real-demo-user");
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Donor_ProfileEndpoint_AlwaysResolvesOwnIdentityServerSide()
    {
        // The profile endpoint never takes a user id from the caller — it always resolves "self"
        // from the validated identity header, so there is no way to target another user's profile.
        var priya = _factory.CreateAuthenticatedClient("demo-priya");

        var upsert = new UpsertDonorProfileRequest(
            BloodGroup.OPositive, "Seattle", AvailabilityStatus.Available, null, ContactPreference.Email, null, true);

        var putResponse = await priya.PutAsJsonAsync("/api/profile", upsert, JsonOptions);
        putResponse.EnsureSuccessStatusCode();

        var priyaProfile = await priya.GetFromJsonAsync<DonorProfileDto>("/api/profile", JsonOptions);
        Assert.NotNull(priyaProfile);
        Assert.Equal("Seattle", priyaProfile!.City);

        // Another authenticated donor querying the same endpoint only ever sees their own profile
        // (from seed data), never Priya's — confirming ownership is resolved server-side per caller.
        var arjunProfile = await _factory.CreateAuthenticatedClient("demo-arjun")
            .GetFromJsonAsync<DonorProfileDto>("/api/profile", JsonOptions);
        Assert.NotNull(arjunProfile);
        Assert.NotEqual("Seattle", arjunProfile!.City);
    }

    [Fact]
    public async Task Requester_CannotViewAnotherUsers_Request()
    {
        var requesterClient = _factory.CreateAuthenticatedClient("demo-liam");
        var create = new CreateBloodRequestDto(
            BloodGroup.BNegative, "General Hospital", "Dublin", 1, UrgencyLevel.Routine, null);

        var createResponse = await requesterClient.PostAsJsonAsync("/api/requests", create, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        var otherClient = _factory.CreateAuthenticatedClient("demo-sofia");
        var otherResponse = await otherClient.GetAsync($"/api/requests/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task MatchedDonor_Identity_OnlyRevealed_AfterAvailableResponse()
    {
        // Ensure a known-compatible, available donor profile exists.
        var donorClient = _factory.CreateAuthenticatedClient("demo-wei");
        await donorClient.PutAsJsonAsync("/api/profile", new UpsertDonorProfileRequest(
            BloodGroup.ONegative, "Bengaluru", AvailabilityStatus.Available, null, ContactPreference.TeamsChat, null, true),
            JsonOptions);

        var requesterClient = _factory.CreateAuthenticatedClient("demo-ana");
        var create = new CreateBloodRequestDto(
            BloodGroup.OPositive, "Metro Hospital", "Bengaluru", 1, UrgencyLevel.Urgent, "Privacy test request");

        var createResponse = await requesterClient.PostAsJsonAsync("/api/requests", create, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        var responsesBefore = await requesterClient.GetFromJsonAsync<List<DonorResponseForRequesterDto>>(
            $"/api/requests/{created!.Id}/responses", JsonOptions);
        Assert.NotEmpty(responsesBefore!);
        Assert.All(responsesBefore!, r => Assert.Null(r.DonorDisplayName));

        var matches = await donorClient.GetFromJsonAsync<List<MatchedRequestDto>>("/api/matches", JsonOptions);
        var myMatch = matches!.First(m => m.RequestId == created.Id);

        var respond = await donorClient.PostAsJsonAsync(
            $"/api/matches/{myMatch.ResponseId}/respond", new RespondToRequestDto(ResponseStatus.Available), JsonOptions);
        respond.EnsureSuccessStatusCode();

        var responsesAfter = await requesterClient.GetFromJsonAsync<List<DonorResponseForRequesterDto>>(
            $"/api/requests/{created.Id}/responses", JsonOptions);
        var revealed = responsesAfter!.First(r => r.ResponseId == myMatch.ResponseId);

        Assert.Equal("Wei Zhang", revealed.DonorDisplayName);
        Assert.False(string.IsNullOrWhiteSpace(revealed.DonorEmail));
    }

    [Fact]
    public async Task Donor_CannotRespond_OnBehalfOfAnotherDonor()
    {
        var donorClient = _factory.CreateAuthenticatedClient("demo-fatima");
        await donorClient.PutAsJsonAsync("/api/profile", new UpsertDonorProfileRequest(
            BloodGroup.APositive, "Hyderabad", AvailabilityStatus.Available, null, ContactPreference.TeamsChat, null, true),
            JsonOptions);

        var requesterClient = _factory.CreateAuthenticatedClient("demo-noah");
        var create = new CreateBloodRequestDto(
            BloodGroup.APositive, "Apollo Hospital", "Hyderabad", 1, UrgencyLevel.Critical, null);
        var createResponse = await requesterClient.PostAsJsonAsync("/api/requests", create, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        var matches = await donorClient.GetFromJsonAsync<List<MatchedRequestDto>>("/api/matches", JsonOptions);
        var myMatch = matches!.First(m => m.RequestId == created!.Id);

        var impostorClient = _factory.CreateAuthenticatedClient("demo-arjun");
        var impostorResponse = await impostorClient.PostAsJsonAsync(
            $"/api/matches/{myMatch.ResponseId}/respond", new RespondToRequestDto(ResponseStatus.Available), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, impostorResponse.StatusCode);
    }

    [Fact]
    public async Task CreateRequest_ValidatesUnitsNeeded()
    {
        var client = _factory.CreateAuthenticatedClient("demo-priya");
        var invalid = new CreateBloodRequestDto(BloodGroup.OPositive, "Hospital", "City", 0, UrgencyLevel.Routine, null);

        var response = await client.PostAsJsonAsync("/api/requests", invalid, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestOwner_CanCancel_ButNotTwice()
    {
        var client = _factory.CreateAuthenticatedClient("demo-sofia");
        var create = new CreateBloodRequestDto(BloodGroup.APositive, "Milan Clinic", "Milan", 1, UrgencyLevel.Routine, null);
        var createResponse = await client.PostAsJsonAsync("/api/requests", create, JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<BloodRequestDto>(JsonOptions);

        var cancelResponse = await client.PostAsync($"/api/requests/{created!.Id}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        var secondCancel = await client.PostAsync($"/api/requests/{created.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, secondCancel.StatusCode);
    }
}
