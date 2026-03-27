using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using BalancerAPI.Api.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<BalancerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ISpecWeightsService, SpecWeightsService>();
builder.Services.AddScoped<ITimeService, TimeService>();
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

app.UseHttpsRedirection();

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
    }
}