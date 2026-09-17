using System.Text.Json.Serialization;
using BloodConnect.Api;
using BloodConnect.Api.Auth;
using BloodConnect.Infrastructure;
using BloodConnect.Infrastructure.Matching;
using BloodConnect.Infrastructure.Notifications;
using BloodConnect.Infrastructure.Options;
using BloodConnect.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
builder.Services.Configure<BloodConnectOptions>(
    builder.Configuration.GetSection(BloodConnectOptions.SectionName));
builder.Services.Configure<TeamsNotificationOptions>(
    builder.Configuration.GetSection($"{BloodConnectOptions.SectionName}:{TeamsNotificationOptions.SectionName}"));

// ---- Data access ----
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=bloodconnect.db";
builder.Services.AddDbContext<BloodConnectDbContext>(options => options.UseSqlite(connectionString));

// ---- Domain/application services ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DonorMatchingService>();
builder.Services.AddScoped<INotificationChannel, LocalNotificationChannel>();
builder.Services.AddHttpClient<INotificationChannel, TeamsWebhookNotificationChannel>();

// ---- Authentication ----
// Local demo scheme by default. To use Entra ID instead, set BloodConnect:EntraId:Enabled=true and
// supply Azure AD tenant/client configuration (see README "Entra ID / Azure configuration") — the
// controllers and ICurrentUserService already depend only on the standard ClaimsPrincipal, so no
// controller code needs to change when swapping schemes.
var demoUserIds = DemoDataSeeder.DemoUsers.Select(u => u.ExternalId).ToArray();

builder.Services
    .AddAuthentication(DemoAuthenticationHandler.SchemeName)
    .AddScheme<DemoAuthenticationOptions, DemoAuthenticationHandler>(
        DemoAuthenticationHandler.SchemeName,
        options => options.KnownExternalIds = demoUserIds);

builder.Services.AddAuthorization();

// ---- MVC / API ----
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BloodConnect API",
        Version = "v1",
        Description = "Employee blood donor network API. Eligibility guidance is informational only; " +
                      "donation center rules always prevail."
    });
    options.AddSecurityDefinition(DemoAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
    {
        Name = "X-Demo-User",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Local demo identity header, e.g. 'demo-priya'. See README for the full list."
    });
});

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloodConnectDbContext>();
    await db.Database.MigrateAsync();

    // Demo data is only ever seeded in Development, never in a real deployment.
    if (app.Environment.IsDevelopment())
    {
        await DemoDataSeeder.SeedAsync(db, CancellationToken.None);
    }
}

app.Run();

/// <summary>
/// Exposed for WebApplicationFactory-based integration tests.
/// </summary>
public partial class Program
{
}
