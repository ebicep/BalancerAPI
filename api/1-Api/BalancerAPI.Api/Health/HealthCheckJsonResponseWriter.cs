using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BalancerAPI.Api.Health;

internal static class HealthCheckJsonResponseWriter
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static Task WriteAsync(HttpContext httpContext, HealthReport report)
	{
		httpContext.Response.ContentType = "application/json; charset=utf-8";

		var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
		var assembly = Assembly.GetEntryAssembly();
		var serviceVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly?.GetName().Version?.ToString();

		var checks = report.Entries.Select(pair =>
		{
			var entry = pair.Value;
			IReadOnlyList<string>? tags = entry.Tags.Any() ? entry.Tags.ToArray() : null;
			return new HealthCheckItem(
				pair.Key,
				entry.Status.ToString(),
				string.IsNullOrWhiteSpace(entry.Description) ? null : entry.Description,
				Math.Round(entry.Duration.TotalMilliseconds, 3),
				entry.Exception?.Message,
				tags);
		}).ToArray();

		var payload = new HealthPayload(
			report.Status.ToString(),
			new HealthServiceInfo("BalancerAPI", env.EnvironmentName, serviceVersion),
			checks,
			Math.Round(report.TotalDuration.TotalMilliseconds, 3));

		return JsonSerializer.SerializeAsync(httpContext.Response.Body, payload, SerializerOptions, httpContext.RequestAborted);
	}

	private sealed record HealthPayload(
		string Status,
		HealthServiceInfo Service,
		IReadOnlyList<HealthCheckItem> Checks,
		double TotalDurationMs);

	private sealed record HealthServiceInfo(
		string Name,
		string Environment,
		string? Version);

	private sealed record HealthCheckItem(
		string Name,
		string Status,
		string? Description,
		double DurationMs,
		string? Error,
		IReadOnlyList<string>? Tags);
}
