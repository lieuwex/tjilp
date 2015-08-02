using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
using TinyTwitter;
using Tweetinvi;
using TweetinviCore.Interfaces.oAuth;
using Keyboard = System.Windows.Input.Keyboard;
using Mouse = System.Windows.Input.Mouse;
using Timer = System.Timers.Timer;

namespace tjilp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public const string TOKEN_CONSUMER_SECRET = "<CONSUMER_SECRET>";
        public const string TOKEN_CONSUMER_KEY = "<CONSUMER_KEY>";
        const string TOKENS_FILE_NAME = "tokens.txt";
        const double ANIMATION_SPEED = .1;
        static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\tjilp";
        public static readonly string FullTokensPath = AppDataPath + "\\" + TOKENS_FILE_NAME;
        public static readonly string FullVersionPath = AppDataPath + "\\version.txt";
        public static readonly string FullTweetDraftPath = AppDataPath + "\\tweetDraft.txt";
        public static readonly string FullTweetDraftsPath = AppDataPath + "\\tweetDrafts.txt";
        public static readonly string FullWindowPositionPath = AppDataPath + "\\windowPosition.txt";
        public static readonly string FullSettingsPath = AppDataPath + "\\tjilp.cfg";
        public static string AccesToken;
        public static string AccesSecret;
        static readonly Timer UpdateTimer = new Timer(1800000);
        public static MainWindow This;
        public static List<TweetDraft> Drafts = new List<TweetDraft>();
        public static int CurrentDraftPosition = -1;
        readonly SolidColorBrush Brush;
        protected readonly double InitialOpacity;
        readonly string RandomMessage;
        readonly Settings Settings = new Settings(FullSettingsPath);
        bool AprilFools;
        bool IsShortening;
        bool StashIsOpen;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
            this.Closed += this.MainWindow_Closed;
            this.InitializeComponent();
            This = this;
            this.InitialOpacity = this.Opacity;
            this.Counter.Content = 140;
            this.RandomMessage = List.Sentences[new Random().Next(List.Sentences.Count)];
            this.Brush = (SolidColorBrush)this.InputBox.Foreground;
            this.InputBox.Text = this.RandomMessage;
            if (File.Exists(FullTweetDraftsPath))
                Drafts = JsonConvert.DeserializeObject<TweetDraft[]>(File.ReadAllText(FullTweetDraftsPath)).ToList();
            if (File.Exists(FullTweetDraftPath))
            {
                this.InputBox.Foreground = Brushes.Black;
                this.Started = this.HasBeenClicked = true;
                this.InputBox.Text = File.ReadAllText(FullTweetDraftPath).Trim();
                File.Delete(FullTweetDraftPath);
            }
            if (File.Exists(FullWindowPositionPath) && File.ReadAllLines(FullWindowPositionPath).Length >= 2 && !File.ReadAllLines(FullWindowPositionPath).Any(p => String.IsNullOrWhiteSpace(p)))
            {
                var info = File.ReadAllLines(FullWindowPositionPath);
                this.Top = Convert.ToDouble(info[0]);
                this.Left = Convert.ToDouble(info[1]);
            }
            this.InputBox.UndoLimit = (new ComputerInfo().AvailablePhysicalMemory / 1000000000) > 2 ? 200 : 100;
            if (!File.Exists(FullTokensPath) || File.ReadAllLines(FullTokensPath).Length < 2 || File.ReadAllLines(FullTokensPath).Any(p => String.IsNullOrWhiteSpace(p)))
            {
                Directory.CreateDirectory(AppDataPath);
                if (File.Exists(TOKENS_FILE_NAME) && File.ReadAllLines(TOKENS_FILE_NAME).Length >= 2 && !File.ReadAllLines(TOKENS_FILE_NAME).Any(p => String.IsNullOrWhiteSpace(p)))
                    File.Move(TOKENS_FILE_NAME, FullTokensPath);
                else
                {
                    var newCredentials = this.GetCredentials();
                    File.WriteAllText(FullTokensPath, newCredentials.AccessToken + MakeNewLine() + newCredentials.AccessTokenSecret);
                }
                if (File.Exists(TOKENS_FILE_NAME)) File.Delete(TOKENS_FILE_NAME);
                SetBlur(ref this.InputBox, 5);
                this.Opacity = 100;
                this.TutBox.Visibility = Visibility.Visible;
                this.TutBox.IsEnabled = true;
                this.InputBox.IsReadOnly = true;
            }
            ReadTokens();
            if (!File.Exists(FullVersionPath)) File.Create(FullVersionPath).Close();
            if (String.IsNullOrWhiteSpace(File.ReadAllText(FullVersionPath)) || Convert.ToUInt16(File.ReadAllText(FullVersionPath)) != App.APP_VERSION)
            {
                SetBlur(ref this.InputBox, 5);
                this.Opacity = 100;
                this.TutBox.Visibility = Visibility.Visible;
                this.TutBox.IsEnabled = true;
                this.InputBox.IsReadOnly = true;
            }
            UpdateTimer.Start();
            UpdateTimer.Elapsed += (s, e) =>
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    if (this.InputBox != null && this.HasBeenClicked && this.Started)
                        File.WriteAllText(FullTweetDraftPath, this.InputBox.Text.Trim());
                    App.UpdateApp();
                    File.Delete(FullTweetDraftPath);
                });
            };
            ThreadPool.QueueUserWorkItem(delegate
            {
                ScheduledTweetHandler.Initialize();
                ScheduledTweetHandler.SendTweets();
            });
            ThreadPool.QueueUserWorkItem(async delegate
            {
                await Task.Delay(5000);
                GC.Collect();
            }); //High Memory bump after initializing, this should fix it.
        }

        public bool IsBeingDragged { get; private set; }
        bool HasBeenClicked { get; set; }
        public bool Started { get; set; }

        public static Task SaveDrafts()
        {
            var tcs = new TaskCompletionSource<bool>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    File.WriteAllText(FullTweetDraftsPath, JsonConvert.SerializeObject(Drafts));
                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        async void MainWindow_Closed(object sender, EventArgs e)
        {
            File.WriteAllText(FullWindowPositionPath, this.Top + Environment.NewLine + this.Left);
            await SaveDrafts();
        }

        static void SetBlur(ref TextBox box, double radius)
        {
            var blurEffect = new BlurEffect();
            blurEffect.Radius = radius;
            blurEffect.KernelType = KernelType.Gaussian;
            box.Effect = blurEffect;
        }

        static void ReadTokens()
        {
            var info = File.ReadAllLines(FullTokensPath);
            AccesToken = info[0];
            AccesSecret = info[1];
        }

        IOAuthCredentials GetCredentials()
        {
            var applicationCredentials = CredentialsCreator.GenerateApplicationCredentials(TOKEN_CONSUMER_KEY, TOKEN_CONSUMER_SECRET);
            var url = CredentialsCreator.GetAuthorizationURL(applicationCredentials);
            Process.Start(url);
            var captcha = InputDialog.Prompt();
            return CredentialsCreator.GetCredentialsFromVerifierCode(captcha, applicationCredentials);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
            try
            {
                App.ParseClient.SendObject("Exceptions", new
                {
                    ExceptionType = exception.GetType().ToString(),
                    ExceptionMessage = exception.Message,
                    Exception = exception.ToString()
                });
                if (this.InputBox != null && this.HasBeenClicked && this.Started)
                    File.WriteAllText(FullTweetDraftPath, this.InputBox.Text.Trim());
            }
            catch (Exception exp)
            {
                MessageBox.Show("Error while sending exception!" + "\nMore info:\n\n" + exp + "\n\nOriginal Exception:\n\n" + exception + "\n\nPlease make a screenshot of this and DM it to @LieuweR", "D:");
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var da = new DoubleAnimation();
                da.From = this.Opacity;
                da.To = 100;
                da.Duration = new Duration(TimeSpan.FromSeconds(100));
                if (!this.MessageGrid.IsEnabled && !this.PopUpGrid.IsEnabled) this.BeginAnimation(OpacityProperty, da);
                this.IsBeingDragged = true;
                if (Mouse.LeftButton == MouseButtonState.Pressed) this.DragMove();
                this.IsBeingDragged = false;
                if (!this.MessageGrid.IsEnabled && !this.PopUpGrid.IsEnabled) this.BeginAnimation(OpacityProperty, null);
            }
            else if (e.ChangedButton == MouseButton.Right) this.WindowState = WindowState.Minimized;
            else if (e.ChangedButton == MouseButton.Middle)
            {
                this.MainWindow_Closed(this, new EventArgs());
                Environment.Exit(0);
            }
        }

        void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.MessageGrid.IsEnabled || this.PopUpGrid.IsEnabled) return;

            var da = new DoubleAnimation
            {
                From = this.Opacity,
                To = this.InitialOpacity,
                Duration = new Duration(TimeSpan.FromSeconds(100))
            };
            this.BeginAnimation(OpacityProperty, da);
        }

        void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.AprilFools && this.Started && this.HasBeenClicked && !string.IsNullOrWhiteSpace(this.InputBox.Text) && DateTime.Today == DateTime.Parse("01-04"))
            {
                var newText = this.InputBox.Text.Substring(0, this.InputBox.Text.Length - 1);
                newText += (char)new Random().Next(76, 123);
                this.InputBox.Text = newText;
                this.InputBox.Select(this.InputBox.Text.Length, 0);
            }

            if (this.InputBox == null || this.Counter == null) return;

            if (this.HasBeenClicked) this.Counter.Content = 140 - this.InputBox.Text.Trim().Length;
            if (this.InputBox.Background != null && this.InputBox.Text.Trim().Length <= 140 && ((SolidColorBrush)this.InputBox.Background).Color == Colors.Red)
            {
                this.Counter.Foreground = new SolidColorBrush(Colors.Red);
                this.Counter.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Red, Colors.Black, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
                this.InputBox.Background = new SolidColorBrush(Colors.Red);
                this.InputBox.Foreground = new SolidColorBrush(Colors.White);
                this.InputBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Red, Colors.White, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
                this.InputBox.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.White, Colors.Black, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
            }
            else if (this.InputBox.Text.Trim().Length > 140 && ((SolidColorBrush)this.InputBox.Background).Color == Colors.White)
            {
                this.Counter.Foreground = new SolidColorBrush(Colors.Black);
                this.Counter.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Black, Colors.Red, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED + .2))));
                this.InputBox.Background = new SolidColorBrush(Colors.White);
                this.InputBox.Foreground = new SolidColorBrush(Colors.Black);
                this.InputBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.White, Colors.Red, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
                this.InputBox.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Black, Colors.White, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
            }
        }

        async void Accept_Click(object sender, RoutedEventArgs e)
        {
            await this.shortenUrls();
            App.ParseClient.TrackCustomEvent("tjilps", new
            {
                charAmmount = this.InputBox.Text.Trim().Length
            });
#if DEBUG
            if (MessageBox.Show("Do you really want to tjilp that?", "DEBUG", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
#endif
            tweetAsync(this.InputBox.Text.Trim());

            this.InputBox.Text = "";
            this.InputBox.IsReadOnly = false;
            this.MessageGrid.Visibility = Visibility.Hidden;
            this.MessageGrid.IsEnabled = false;
            this.InputBox.Focus();
            this.ShowPopUpMessage("succesfully tjilped", Colors.Black);
        }

        static Task tweetAsync(string content) { return Task.Factory.StartNew(() => { TinyTwitter.TinyTwitter.UpdateStatus(new OAuthInfo(TOKEN_CONSUMER_KEY, TOKEN_CONSUMER_SECRET, AccesToken, AccesSecret), content); }); }

        void Decline_Click(object sender, RoutedEventArgs e)
        {
            this.InputBox.IsReadOnly = false;
            var da = new DoubleAnimation
            {
                From = this.Opacity,
                To = this.InitialOpacity,
                Duration = new Duration(TimeSpan.FromSeconds(.01))
            };
            this.BeginAnimation(OpacityProperty, da);
            this.MessageGrid.Visibility = Visibility.Hidden;
            this.MessageGrid.IsEnabled = false;
            this.InputBox.Focus();
        }

        async void TutBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Middle && e.ChangedButton != MouseButton.Right) return;

            this.TutBox.IsEnabled = false;
            this.TutBox.Visibility = Visibility.Hidden;
            this.Opacity = this.InitialOpacity;
            this.InputBox.IsReadOnly = false;
            this.InputBox.Effect.ResetBlurWithAnimation();
            if (String.IsNullOrWhiteSpace(File.ReadAllText(FullVersionPath)))
            {
                File.WriteAllText(FullVersionPath, App.APP_VERSION.ToString());
                this.ShowPopUpMessage("welcome to tjilp", Colors.Black);
            }
            else if (Convert.ToUInt16(File.ReadAllText(FullVersionPath)) != App.APP_VERSION)
            {
                this.AprilFools = true;
                File.WriteAllText(FullVersionPath, App.APP_VERSION.ToString());
                await this.ShowPopUpMessage("tjilp has been updated", Colors.Black, 2);
                foreach(var item in App.ChangeLog)
                    await this.ShowPopUpMessage(item, Colors.Black, .75);
            }
        }

        async void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) e.Handled = false;
                    else 
                    {
                        e.Handled = true;
                        if (!String.IsNullOrWhiteSpace(this.InputBox.Text))
                        {
                            if (this.InputBox.Text.Trim().Length <= 140)
                            {
                                this.InputBox.IsReadOnly = true;
                                {
                                    var da = new DoubleAnimation
                                    {
                                        From = this.InitialOpacity,
                                        To = 100,
                                        Duration = new Duration(TimeSpan.FromSeconds(20))
                                    };
                                    this.BeginAnimation(OpacityProperty, da);
                                }
                                {
                                    this.MessageGrid.Visibility = Visibility.Visible;
                                    this.MessageGrid.IsEnabled = true;
                                    this.Decline.Focus();
                                    var da = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 100,
                                        Duration = new Duration(TimeSpan.FromSeconds(20))
                                    };
                                    this.MessageGrid.BeginAnimation(OpacityProperty, da);
                                }
                            }
                            else
                                await this.ShowPopUpMessage("More than 140 characters", Colors.Red);
                        }
                    }
                    break;
                case Key.Escape: {
                    this.TextSizeSlider.Value = this.InputBox.FontSize;
                    this.TopBox.SelectedIndex = (this.Topmost) ? 0 : 1;
                    this.SettingsPanel.Visibility = Visibility.Visible;
                    this.SettingsPanel.IsEnabled = true;
                    var da = new DoubleAnimation
                    {
                        From = this.SettingsPanel.Opacity,
                        To = 100,
                        Duration = new Duration(TimeSpan.FromSeconds(15))
                    };
                    this.SettingsPanel.BeginAnimation(OpacityProperty, da);
                    CreditsTimer.Start();
                    CreditsTimer.Elapsed += this.CreditsTimer_Elapsed;
                }
                    break;
                case Key.Space:
                    this.shortenUrls();
                    break;
                default:
                    if (this.PopUpGrid.IsEnabled) this.hideMessage(false);
                    break;
            }
        }

        async Task shortenUrls()
        {
            await Task.Factory.StartNew(async () =>
            {
                if (this.IsShortening) return;
                this.IsShortening = true;

                string inputBoxText = null;
                await this.Dispatcher.InvokeAsync(() => { inputBoxText = this.InputBox.Text; });
                var urls = inputBoxText.Split(' ')
                                       .Where(s => !string.IsNullOrWhiteSpace(s.Replace("nl", "").Replace("com", "").Replace("www", "").Replace(".", "")) &&
                                              Uri.IsWellFormedUriString(s, UriKind.Absolute) ||
                                              s.Contains("www.", StringComparison.InvariantCultureIgnoreCase) ||
                                              s.Contains(".nl", StringComparison.InvariantCultureIgnoreCase) ||
                                              s.Contains(".com", StringComparison.InvariantCultureIgnoreCase));
                Parallel.ForEach(urls, async u =>
                {
                    var shortUrl = await new WebClient().DownloadStringTaskAsync("http://is.gd/create.php?format=simple&url=" + HttpUtility.UrlEncode(u));
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        if (Uri.IsWellFormedUriString(shortUrl, UriKind.Absolute)) //Replace long URL with short one & strip 'http://' since it's not needed.
                            this.InputBox.Text = this.InputBox.Text.Replace(u, shortUrl.Substring(7));
                        this.InputBox.Select(this.InputBox.Text.Length, 0);
                    });
                });
                this.IsShortening = false;
            });
        }

        static string MakeNewLine(int ammount = 1)
        {
            if (ammount == 0) return "";
            if (ammount == 1) return Environment.NewLine;
            var sb = new StringBuilder();
            for (var i = 0; i < ammount; i++)
                sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        void InputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.Started = true;
            if (!this.HasBeenClicked)
            {
                this.HasBeenClicked = true;
                this.InputBox.Text = "";
                this.InputBox.Foreground = Brushes.Black;
            }
        }

        void InputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.HasBeenClicked && String.IsNullOrEmpty(this.InputBox.Text))
            {
                this.HasBeenClicked = false;
                this.InputBox.Text = this.RandomMessage;
                this.InputBox.Foreground = this.Brush;
            }
        }

        void tjilpWindow_Deactivated(object sender, EventArgs e) { this.InputBox_LostFocus(sender, new RoutedEventArgs()); }
        void tjilpWindow_Activated(object sender, EventArgs e) { if (this.Started) this.InputBox_GotFocus(sender, new RoutedEventArgs()); }

        void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 50) this.StashGrid_ShowPrevious();
            else if (e.Delta < -50) this.StashGrid_ShowNext();
        }

        void StashGrid_ShowNext()
        {
            if (CurrentDraftPosition < Drafts.Count - 1) CurrentDraftPosition++;
            this.StashGrid_UpdateView();
        }

        void StashGrid_ShowPrevious()
        {
            if (CurrentDraftPosition != 0) CurrentDraftPosition--;
            else
            {
                if (!this.StashIsOpen) return;
                this.StashIsOpen = false;
                var transform = new TranslateTransform();
                var da = new DoubleAnimation
                {
                    From = this.StashGrid.RenderTransform.Value.OffsetY,
                    To = 190,
                    Duration = new Duration(TimeSpan.FromSeconds(.1)),
                    FillBehavior = FillBehavior.Stop
                };
                da.Completed += (s, e) =>
                {
                    this.StashGrid.IsEnabled = false;
                    this.StashGrid.Visibility = Visibility.Hidden;
                    this.StashGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(this.StashGrid.Opacity, 0, new Duration(TimeSpan.FromSeconds(.01))));
                    this.Counter.Foreground = (this.InputBox.Text.Trim().Length > 140) ? Brushes.Red : Brushes.Black;
                    this.Counter.Content = (this.Started && this.HasBeenClicked) ? (140 - this.InputBox.Text.Trim().Length).ToString() : "140";
                };
                transform.BeginAnimation(TranslateTransform.YProperty, da);
                this.StashGrid.RenderTransform = transform;
                CurrentDraftPosition = 0;
                SaveDrafts();
            }
            this.StashGrid_UpdateView();
        }

        void StashGrid_UpdateView()
        {
            if (Drafts.Count == 0)
            {
                this.StashIsOpen = false;
                this.Counter.Foreground = (this.InputBox.Text.Trim().Length > 140) ? Brushes.Red : Brushes.Black;
                this.Counter.Content = (this.Started && this.HasBeenClicked) ? (140 - this.InputBox.Text.Trim().Length).ToString() : "140";
                this.StashGrid.IsEnabled = false;
                this.StashGrid.Visibility = Visibility.Hidden;
                var da = new DoubleAnimation
                {
                    From = this.StashGrid.Opacity,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(.01))
                };
                this.StashGrid.BeginAnimation(OpacityProperty, da);
                this.InputBox.Focus();
                SaveDrafts();
            }
            else
            {
                try
                {
                    var currentDraft = Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition);
                    this.TweetLabel.Text = "\"" + currentDraft.Tweet + "\"";
                    switch ((DateTime.Today - currentDraft.DateTime.Date).Days)
                    {
                        case 6:
                        case 5:
                        case 4:
                        case 3:
                        case 2:
                            this.DateLabel.Content = currentDraft.DateTime.DayOfWeek + " " + currentDraft.DateTime.ToString("HH:mm");
                            break;
                        case 1:
                            this.DateLabel.Content = "yesterday " + currentDraft.DateTime.ToString("HH:mm");
                            break;
                        case 0:
                            this.DateLabel.Content = "today " + currentDraft.DateTime.ToString("HH:mm");
                            break;
                        default:
                            this.DateLabel.Content = currentDraft.DateTime.ToString("yyy-MM-dd");
                            break;
                    }
                    this.Counter.Foreground = Brushes.Black;
                    this.Counter.Content = (CurrentDraftPosition + 1) + "/" + Drafts.Count;
                    if (this.DateLabel.IsMouseOver) this.DateLabel.Content = "click to schedule";
                }
                catch (ArgumentOutOfRangeException)
                {
                    CurrentDraftPosition = 0;
                    this.StashGrid_UpdateView();
                }
            }
        }

        void TweetLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.StashIsOpen = false;
            if (e.ChangedButton != MouseButton.Left) return;
            this.InputBox.Foreground = Brushes.Black;
            this.Started = this.HasBeenClicked = true;
            this.InputBox.Text = Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).Tweet;
            this.InputBox.Select(this.InputBox.Text.Length, 0);
            this.StashGrid.IsEnabled = false;
            this.StashGrid.Visibility = Visibility.Hidden;
            var da = new DoubleAnimation
            {
                From = this.StashGrid.Opacity,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(.01))
            };
            this.StashGrid.BeginAnimation(OpacityProperty, da);
            SaveDrafts();
        }

        void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Drafts.RemoveAt((Drafts.Count - 1) - CurrentDraftPosition);
                SaveDrafts();
            }
            catch (ArgumentOutOfRangeException)
            {
                if (Drafts.Count == 1)
                {
                    Drafts.RemoveAt(0);
                    SaveDrafts();
                }
            }
            finally
            {
                this.StashGrid_UpdateView();
            }
        }

        void InputBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!this.StashIsOpen && e.Delta < -50)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (!String.IsNullOrWhiteSpace(this.InputBox.Text) && this.Started && this.HasBeenClicked)
                    {
                        Drafts.Add(new TweetDraft
                        {
                            Tweet = this.InputBox.Text.Trim(),
                            DateTime = DateTime.Now
                        });
                        this.InputBox.Text = "";
                        SaveDrafts();
                    }
                }
                if (Drafts.Count != 0)
                {
                    this.StashIsOpen = true;
                    CurrentDraftPosition = 0;
                    this.StashGrid_UpdateView();
                    this.StashGrid.IsEnabled = true;
                    this.StashGrid.Visibility = Visibility.Visible;
                    this.StashGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(this.StashGrid.Opacity, 100, new Duration(TimeSpan.FromMilliseconds(0.1))));
                    var transform = new TranslateTransform();
                    var da = new DoubleAnimation
                    {
                        From = 190,
                        To = 0,
                        Duration = new Duration(TimeSpan.FromSeconds(.1)),
                        FillBehavior = FillBehavior.Stop
                    };
                    transform.BeginAnimation(TranslateTransform.YProperty, da);
                    this.StashGrid.RenderTransform = transform;
                }
            }
        }

        void DateLabel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.DateLabel.BeginAnimation(FontSizeProperty, new DoubleAnimation(23, 19, new Duration(TimeSpan.FromSeconds(.05))));
            this.DateLabel.Content = "click to schedule";
        }

        void DateLabel_MouseLeave(object sender, MouseEventArgs e)
        {
            this.DateLabel.BeginAnimation(FontSizeProperty, new DoubleAnimation(19, 23, new Duration(TimeSpan.FromSeconds(.05))));
            this.StashGrid_UpdateView();
        }
    }

    public static class Extensions
    {
        public static void ResetBlurWithAnimation(this Effect effect)
        {
            var blurEffect = effect as BlurEffect;
            if (blurEffect == null) throw new ArgumentException("Argument has to be BlurEffect.");
            var da = new DoubleAnimation
            {
                From = blurEffect.Radius,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(.25))
            };
            effect.BeginAnimation(BlurEffect.RadiusProperty, da);
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null || action == null) throw new ArgumentNullException();
            foreach(var item in collection) action(item);
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp) { return source.IndexOf(toCheck, comp) >= 0; }
    }
}