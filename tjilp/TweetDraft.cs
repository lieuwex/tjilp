using System;
using System.Collections.Generic;

namespace tjilp
{
    sealed public class TweetDraft
    {
        public string Tweet { get; set; }
        public DateTime DateTime { get; set; }
        public ScheduledTweet ScheduledTweet { get; set; }
    }

    sealed public class ScheduledTweet
    {
        public string Name { get; set; }
        public DateTime LastRun { get; set; }
        public RepeatMode RepeatMode { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; }
        public List<DateTime> Dates { get; set; }
        public DateTime Time { get; set; }
    }

    public enum RepeatMode : int
    {
        None = 0,
        DayOfWeek = 1,
        Date = 2,
        Time = 3
    }
}
