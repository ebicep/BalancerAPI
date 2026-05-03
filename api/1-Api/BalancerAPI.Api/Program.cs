using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using BalancerAPI.Api.Authentication;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Api.Routing;
using BalancerAPI.Api.Security;
using BalancerAPI.Api.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ApiKeyOptions>, ApiKeyOptionsValidator>();

builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = ApiKeyAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
        .RequireAuthenticatedUser()
        .Build();
    options.AddApiPermissionPolicies();
});

builder.Services.AddControllers(options =>
{
    // Ensures [controller] and [action] route tokens are kebab-cased globally.
    options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));
});
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new MediaTypeApiVersionReader("version") // Accept/Content-Type: application/json;version=1.0 
        );
    })
    .AddApiExplorer(options =>
    {
        // Results in swagger documents named like "v1", "v2", etc.
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BalancerDbContext>(options =>
    options.UseNpgsql(connectionString));
// Scoped factory so it can use the same scoped DbContextOptions registration as AddDbContext.
builder.Services.AddDbContextFactory<BalancerDbContext>(
    options => options.UseNpgsql(connectionString),
    ServiceLifetime.Scoped);
builder.Services.AddScoped<ISpecWeightsService, SpecWeightsService>();
builder.Services.AddScoped<IExperimentalBalanceService, ExperimentalBalanceService>();
builder.Services.AddScoped<IExperimentalBalanceConfirmService, ExperimentalBalanceConfirmService>();
builder.Services.AddScoped<IExperimentalBalanceInputService, ExperimentalBalanceInputService>();
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAdjustmentAutoDailyService, AdjustmentAutoDailyService>();
builder.Services.AddScoped<IAdjustmentAutoWeeklyService, AdjustmentAutoWeeklyService>();
builder.Services.AddScoped<IManualWeightAdjustmentService, ManualWeightAdjustmentService>();
builder.Services.AddHttpClient<INameUpdateService, NameUpdateService>(client => { client.BaseAddress = new Uri("https://sessionserver.mojang.com/"); });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        // Create one Swagger endpoint per discovered API version.
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

// In Development, HTTP URLs (e.g. http://localhost:5099) must stay HTTP — otherwise
// clients like Node fetch follow redirects to HTTPS and hit dev cert trust issues.
if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        // Add one Swagger document per discovered API version.
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "BalancerAPI",
                Version = description.ApiVersion.ToString()
            });
        }

        // Ensure operations are placed into the correct version document.
        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            var groupName = apiDesc.GroupName;
            return string.Equals(groupName, docName, StringComparison.OrdinalIgnoreCase);
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "API Key",
            Description = "Authorization: Bearer bkr_<guid>_<secret>"
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        options.OperationFilter<AuthErrorResponsesOperationFilter>();
    }
}