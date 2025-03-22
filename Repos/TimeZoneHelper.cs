namespace Propertease.Repos
{
    public static class TimeZoneHelper
    {
        public static DateTime ToNepalTime(this DateTime utcTime)
        {
            // Nepal's time offset is UTC+5:45
            TimeSpan nepalOffset = new TimeSpan(5, 45, 0);
            return utcTime.Add(nepalOffset);
        }

        public static DateTime ToUtcFromNepal(this DateTime nepalTime)
        {
            // Convert Nepal time to UTC by subtracting the offset
            TimeSpan nepalOffset = new TimeSpan(5, 45, 0);
            return nepalTime.Subtract(nepalOffset);
        }
    }
}
