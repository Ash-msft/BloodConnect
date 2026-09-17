using System.Text.Json;
using BloodConnect.Domain;

namespace BloodConnect.Infrastructure.Notifications;

/// <summary>
/// Builds Microsoft Adaptive Card JSON payloads for BloodConnect notifications. The same card is used
/// for both the local outbox preview and (when configured) actual Teams delivery, so what a developer
/// sees locally is exactly what would be sent to Teams.
/// </summary>
public static class AdaptiveCardBuilder
{
    public static string BuildNewRequestCard(BloodRequest request, string? appBaseUrl)
    {
        var respondUrl = appBaseUrl is null
            ? null
            : $"{appBaseUrl.TrimEnd('/')}/requests/{request.Id}";

        var card = new
        {
            type = "AdaptiveCard",
            version = "1.4",
            body = new object[]
            {
                new { type = "TextBlock", text = "🩸 Blood Donation Request", weight = "Bolder", size = "Medium" },
                new
                {
                    type = "TextBlock",
                    wrap = true,
                    text = $"A colleague needs **{FormatBloodGroup(request.BloodGroup)}** blood " +
                           $"({request.UnitsNeeded} unit(s)) at **{request.HospitalName}**, {request.City}. " +
                           $"Urgency: {request.Urgency}."
                },
                new { type = "TextBlock", wrap = true, isSubtle = true, text = request.Notes ?? string.Empty }
            },
            actions = BuildActions(request.Id, respondUrl)
        };

        return JsonSerializer.Serialize(card, new JsonSerializerOptions { WriteIndented = false });
    }

    private static object[] BuildActions(Guid requestId, string? respondUrl)
    {
        var actions = new List<object>
        {
            new
            {
                type = "Action.Submit",
                title = "I'm Available",
                data = new { requestId, response = "Available" }
            },
            new
            {
                type = "Action.Submit",
                title = "Not Available",
                data = new { requestId, response = "NotAvailable" }
            }
        };

        if (respondUrl is not null)
        {
            actions.Add(new { type = "Action.OpenUrl", title = "Open in BloodConnect", url = respondUrl });
        }

        return actions.ToArray();
    }

    private static string FormatBloodGroup(BloodGroup group) => group switch
    {
        BloodGroup.OPositive => "O+",
        BloodGroup.ONegative => "O-",
        BloodGroup.APositive => "A+",
        BloodGroup.ANegative => "A-",
        BloodGroup.BPositive => "B+",
        BloodGroup.BNegative => "B-",
        BloodGroup.ABPositive => "AB+",
        BloodGroup.ABNegative => "AB-",
        _ => group.ToString()
    };
}
