using System.Text;
using BalancerAPI.Common.Auth;
using BalancerAPI.Common.Security;
using BalancerAPI.Data.Data;
using BalancerAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

const int minPepperLength = 32;

if (args.Length < 1 || !string.Equals(args[0], "create", StringComparison.OrdinalIgnoreCase) || args.Length < 3)
{
    PrintUsage();
    return 1;
}

var name = args[1];
var roleArgs = args.Skip(2).ToList();
if (string.IsNullOrWhiteSpace(name) || roleArgs.Count == 0)
{
    PrintUsage();
    return 1;
}

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection");
var pepper = config["Authentication:ApiKey:Pepper"];
var pepperVersionStr = config["Authentication:ApiKey:PepperVersion"];

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Set ConnectionStrings:DefaultConnection (env var or user-secrets).");
    return 1;
}

if (string.IsNullOrEmpty(pepper) || pepper.Length < minPepperLength)
{
    Console.Error.WriteLine(
        $"Set Authentication:ApiKey:Pepper to a value of at least {minPepperLength} characters (env var or user-secrets).");
    return 1;
}

var pepperVersion = 1;
if (!string.IsNullOrEmpty(pepperVersionStr) && !int.TryParse(pepperVersionStr, out pepperVersion))
{
    Console.Error.WriteLine("Authentication:ApiKey:PepperVersion must be an integer.");
    return 1;
}
if (pepperVersion < 1)
{
    Console.Error.WriteLine("Authentication:ApiKey:PepperVersion must be >= 1.");
    return 1;
}

var (fullKey, publicId) = ApiKeyFormat.GenerateNewKey();
if (!ApiKeyFormat.TryParse(fullKey, out var parsedId, out var secret) || parsedId != publicId)
{
    throw new InvalidOperationException("Generated key could not be parsed.");
}

var secretHash = ApiKeyHasher.HashSecret(secret, pepper);

await using var db = new BalancerDbContext(
    new DbContextOptionsBuilder<BalancerDbContext>()
        .UseNpgsql(connectionString)
        .Options);

await db.Database.MigrateAsync();

var roles = roleArgs.Select(r => r.Trim()).Where(r => r.Length > 0).ToList();
if (!RolePermissions.TryResolvePermissions(roles, out _))
{
    Console.Error.WriteLine("Unknown role(s). Known roles: BotFull, WebReadOnly");
    return 1;
}

db.ApiClients.Add(new ApiClient
{
    Id = publicId,
    Name = name,
    SecretHash = secretHash,
    PepperVersion = pepperVersion,
    Roles = [.. roles],
    CreatedAt = DateTimeOffset.UtcNow,
    RevokedAt = null
});

await db.SaveChangesAsync();

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine($"Created API client (pepper_version={pepperVersion}). Save this key now (shown once):");
Console.WriteLine(fullKey);
return 0;

static void PrintUsage()
{
    Console.WriteLine("Usage: BalancerAPI.Tools create <display-name> <api-role> [<api-role> ...]");
    Console.WriteLine("Example: dotnet run --project api/1-Tools/BalancerAPI.Tools -- create \"Discord Bot\" BotFull");
    Console.WriteLine("Reads ConnectionStrings:DefaultConnection, Authentication:ApiKey:Pepper, and");
    Console.WriteLine("Authentication:ApiKey:PepperVersion (default 1) from configuration / env / user-secrets.");
}
