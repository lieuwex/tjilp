using System;
using System.Linq;
using System.Timers;
using TinyTwitter;

namespace tjilp
{
    internal static class ScheduledTweetHandler
    {
        static Timer t;

        public static void Initialize(double secondBetweenCheck = 60)
        {
            t = new Timer(secondBetweenCheck * 1000);
            t.Start();
            t.Elapsed += (s, e) => { SendTweets(); };
        }

        public static async void SendTweets()
        {
            t.Stop();
            foreach(var draft in MainWindow.Drafts.Where(x => x.ScheduledTweet != null))
            {
                if (draft.ScheduledTweet.RepeatMode == RepeatMode.None && draft.ScheduledTweet.Dates.Single() <= DateTime.Today && draft.ScheduledTweet.Time.Hour <= DateTime.Now.Hour && draft.ScheduledTweet.Time.Minute <= DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(new OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY, MainWindow.TOKEN_CONSUMER_SECRET, MainWindow.AccesToken, MainWindow.AccesSecret), draft.Tweet);
                    draft.ScheduledTweet = null;
                    await MainWindow.SaveDrafts();
                }
                else if (draft.ScheduledTweet.RepeatMode == RepeatMode.Time && draft.ScheduledTweet.LastRun.AddHours(draft.ScheduledTweet.Time.Hour).Hour == DateTime.Now.Hour && draft.ScheduledTweet.LastRun.AddMinutes(draft.ScheduledTweet.Time.Minute).Minute == DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(new OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY, MainWindow.TOKEN_CONSUMER_SECRET, MainWindow.AccesToken, MainWindow.AccesSecret), draft.Tweet);
                    draft.ScheduledTweet.LastRun = DateTime.Now;
                    await MainWindow.SaveDrafts();
                }
                else if (draft.ScheduledTweet.RepeatMode == RepeatMode.DayOfWeek && draft.ScheduledTweet.DaysOfWeek.Contains(DateTime.Today.DayOfWeek) && draft.ScheduledTweet.Time.Hour == DateTime.Now.Hour && draft.ScheduledTweet.Time.Minute == DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(new OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY, MainWindow.TOKEN_CONSUMER_SECRET, MainWindow.AccesToken, MainWindow.AccesSecret), draft.Tweet);
                    draft.ScheduledTweet.LastRun = DateTime.Now;
                    await MainWindow.SaveDrafts();
                }
                else if (draft.ScheduledTweet.RepeatMode == RepeatMode.Date && draft.ScheduledTweet.Dates.Contains(DateTime.Today) && draft.ScheduledTweet.Time.Hour == DateTime.Now.Hour && draft.ScheduledTweet.Time.Minute == DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(new OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY, MainWindow.TOKEN_CONSUMER_SECRET, MainWindow.AccesToken, MainWindow.AccesSecret), draft.Tweet);
                    draft.ScheduledTweet.LastRun = DateTime.Now;
                    await MainWindow.SaveDrafts();
                }
            }
            t.Start();
        }
    }
}