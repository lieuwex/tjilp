using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace tjilp
{
    static class ScheduledTweetHandler
    {
        private static Timer t;

        public static void Initialize(double secondBetweenCheck = 60)
        {
            t = new Timer(secondBetweenCheck * 1000);
            t.Elapsed += (s, e) =>
            {
                SendTweets();
            };
        }

        public static void SendTweets()
        {
            foreach (var tweet in MainWindow.Drafts.Where(t => t.ScheduledTweet != null))
            {
                if (tweet.ScheduledTweet.RepeatMode == RepeatMode.None &&
                    tweet.ScheduledTweet.Dates.Single() <= DateTime.Today &&
                    tweet.ScheduledTweet.Time.Hour <= DateTime.Now.Hour &&
                    tweet.ScheduledTweet.Time.Minute <= DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(
                        new TinyTwitter.OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY,
                            MainWindow.TOKEN_CONSUMER_SECRET,
                            MainWindow.AccesToken,
                            MainWindow.AccesSecret),
                        tweet.Tweet);
                    tweet.ScheduledTweet = null;
                    MainWindow.SaveDrafts();
                }

                else if (tweet.ScheduledTweet.RepeatMode == RepeatMode.Time &&
                    tweet.ScheduledTweet.LastRun.AddHours(tweet.ScheduledTweet.Time.Hour).Hour == DateTime.Now.Hour &&
                    tweet.ScheduledTweet.LastRun.AddMinutes(tweet.ScheduledTweet.Time.Minute).Minute == DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(
                        new TinyTwitter.OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY,
                            MainWindow.TOKEN_CONSUMER_SECRET,
                            MainWindow.AccesToken,
                            MainWindow.AccesSecret),
                        tweet.Tweet);
                    tweet.ScheduledTweet.LastRun = DateTime.Now;
                    MainWindow.SaveDrafts();
                }

                else if (tweet.ScheduledTweet.RepeatMode == RepeatMode.DayOfWeek &&
                    tweet.ScheduledTweet.DaysOfWeek.Contains(DateTime.Today.DayOfWeek) &&
                    tweet.ScheduledTweet.Time.Hour == DateTime.Now.Hour &&
                    tweet.ScheduledTweet.Time.Minute == DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(
                     new TinyTwitter.OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY,
                         MainWindow.TOKEN_CONSUMER_SECRET,
                         MainWindow.AccesToken,
                         MainWindow.AccesSecret),
                     tweet.Tweet);
                    tweet.ScheduledTweet.LastRun = DateTime.Now;
                    MainWindow.SaveDrafts();
                }

                else if (tweet.ScheduledTweet.RepeatMode == RepeatMode.Date &&
                    tweet.ScheduledTweet.Dates.Contains(DateTime.Today) &&
                    tweet.ScheduledTweet.Time.Hour == DateTime.Now.Hour &&
                    tweet.ScheduledTweet.Time.Minute == DateTime.Now.Minute)
                {
                    TinyTwitter.TinyTwitter.UpdateStatus(
                    new TinyTwitter.OAuthInfo(MainWindow.TOKEN_CONSUMER_KEY,
                        MainWindow.TOKEN_CONSUMER_SECRET,
                        MainWindow.AccesToken,
                        MainWindow.AccesSecret),
                    tweet.Tweet);
                    tweet.ScheduledTweet.LastRun = DateTime.Now;
                    MainWindow.SaveDrafts();
                }
            }
        }
    }
}
