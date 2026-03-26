using Asp.Versioning;
using BalancerAPI.Business.Services;
using BalancerAPI.Data.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
});
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BalancerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ISpecWeightsService, SpecWeightsService>();
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddHttpClient<INameUpdateService, NameUpdateService>(client =>
{
    client.BaseAddress = new Uri("https://sessionserver.mojang.com/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();