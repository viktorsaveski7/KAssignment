using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Claims.Infrastructure.Persistence.Configurations;

public sealed class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
    public static readonly DateOnlyConverter Instance = new();

    public DateOnlyConverter()
        : base(
            date => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            dateTime => DateOnly.FromDateTime(dateTime))
    {
    }
}
