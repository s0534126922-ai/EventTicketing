
using EventTicketing.Core.Exceptions;

namespace EventTicketing.Service.Helpers;

public static class IsraelTimeHelper
{
    private static readonly TimeZoneInfo IsraelZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Jerusalem");

    public static DateTime UtcNow =>
        DateTime.UtcNow;

    public static DateTime IsraelNow =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, IsraelZone);

    public static DateTime ToUtc(DateTime israelTime)
    {
        if (israelTime.Kind == DateTimeKind.Utc)
        {
            throw new ValidationException(
                "Event time must be provided in Israel local time.");
        }

        var localTime = DateTime.SpecifyKind(
            israelTime, DateTimeKind.Unspecified);

        if (IsraelZone.IsInvalidTime(localTime))
        {
            throw new ValidationException(
                "The selected time does not exist in Israel.");
        }

        if (IsraelZone.IsAmbiguousTime(localTime))
        {
            throw new ValidationException(
                "The selected time is ambiguous in Israel.");
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            localTime, IsraelZone);
    }

    public static DateTime ToIsrael(DateTime utcTime)
    {
        var normalizedUtc = DateTime.SpecifyKind(
            utcTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(
            normalizedUtc, IsraelZone);
    }
}
