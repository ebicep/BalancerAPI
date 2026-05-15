using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BalancerAPI.Data.Converters;

/// <summary>
/// Ensures <see cref="DateTime"/> values written to PostgreSQL timestamptz use <see cref="DateTimeKind.Utc"/>.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => NormalizeToUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
}
