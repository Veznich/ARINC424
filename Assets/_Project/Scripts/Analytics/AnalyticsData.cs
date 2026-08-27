using System;
using System.Collections.Generic;

namespace Arkanoid.Analytics
{
    /// <summary>DTO для Newtonsoft JSON (не Unity serializer).</summary>
    public sealed class AnalyticsEventDto
    {
        public string name;
        public string utc;
        public Dictionary<string, string> props;

        public static AnalyticsEventDto Create(string name, Dictionary<string, string> props = null)
        {
            return new AnalyticsEventDto
            {
                name = name,
                utc = DateTime.UtcNow.ToString("o"),
                props = props ?? new Dictionary<string, string>()
            };
        }
    }

    public sealed class AnalyticsCounters
    {
        public int sessions;
        public int levelsStarted;
        public int levelsCompleted;
        public int ballsLost;
        public int powerUpsCollected;
        public int gameOvers;
        public int replaysSaved;
        public int replaysPlayed;
        public int maxLevelReached = 1;
    }

    public sealed class AnalyticsData
    {
        public int version = 1;
        public string lastUpdatedUtc;
        public AnalyticsCounters counters = new AnalyticsCounters();
        public List<AnalyticsEventDto> events = new List<AnalyticsEventDto>(128);

        public static AnalyticsData CreateDefault()
        {
            return new AnalyticsData
            {
                lastUpdatedUtc = DateTime.UtcNow.ToString("o"),
                counters = new AnalyticsCounters(),
                events = new List<AnalyticsEventDto>(128)
            };
        }
    }
}
