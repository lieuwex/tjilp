using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Tweetinvi;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace tjilp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly double InitialOpacity;
        public const string TOKEN_CONSUMER_SECRET = "<CONSUMER_SECRET>";
        public const string TOKEN_CONSUMER_KEY = "<CONSUMER_KEY>";

        private const string TOKENS_FILE_NAME = "tokens.txt";
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\tjilp";
        public static readonly string FullTokensPath = AppDataPath + "\\" + TOKENS_FILE_NAME;
        public static readonly string FullErrorPath = AppDataPath + "\\error.txt";
        public static readonly string FullVersionPath = AppDataPath + "\\version.txt";
        public static readonly string FullTweetDraftPath = AppDataPath + "\\tweetDraft.txt";
        public static readonly string FullTweetDraftsPath = AppDataPath + "\\tweetDrafts.txt";
        public static readonly string FullWindowPositionPath = AppDataPath + "\\windowPosition.txt";
        public static readonly string FullSettingsPath = AppDataPath + "\\tjilp.cfg";

        public static string AccesToken;
        public static string AccesSecret;

        public bool IsBeingDragged { get; private set; }
        private bool HasBeenClicked { get; set; }

        private readonly string RandomMessage;
        private readonly SolidColorBrush Brush;

        private static readonly Timer UpdateTimer = new Timer(1800000);

        public static MainWindow This;

        private static readonly Dictionary<string, string> Settings = new Dictionary<string, string>();

        public static List<TweetDraft> Drafts = new List<TweetDraft>();
        public static int CurrentDraftPosition = -1;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Closed += MainWindow_Closed;

            InitializeComponent();

            This = this;

            this.InitialOpacity = this.Opacity;
            this.Counter.Content = 140;

            RandomMessage = List.Sentences[new Random().Next(List.Sentences.Count)];
            Brush = (SolidColorBrush)InputBox.Foreground;
            InputBox.Text = RandomMessage;

            if (File.Exists(FullTweetDraftsPath)) Drafts = JsonConvert.DeserializeObject<TweetDraft[]>(File.ReadAllText(FullTweetDraftsPath)).ToList();

            if(File.Exists(FullTweetDraftPath))
            {
                InputBox.Foreground = new SolidColorBrush(Colors.Black);
                Started = HasBeenClicked = true;

                InputBox.Text = File.ReadAllText(FullTweetDraftPath).Trim();
                File.Delete(FullTweetDraftPath);
            }

            if (File.Exists("error.txt") && File.GetLastWriteTime("error.txt").Date < DateTime.Today.AddDays(-2)) File.Delete("error.txt");

            if (File.Exists(FullWindowPositionPath) && File.ReadAllLines(FullWindowPositionPath).Length >= 2 && File.ReadAllLines(FullWindowPositionPath).Any(p => !String.IsNullOrWhiteSpace(p)))
            {
                var info = File.ReadAllLines(FullWindowPositionPath);
                this.Top = Convert.ToDouble(info[0]);
                this.Left = Convert.ToDouble(info[1]);
            }

            if ((new ComputerInfo().AvailablePhysicalMemory / 1000000000) > 2) InputBox.UndoLimit = 200;
            else InputBox.UndoLimit = 100;

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

                SetBlur(ref InputBox, 5);
                this.Opacity = 100;
                TutBox.Visibility = System.Windows.Visibility.Visible;
                TutBox.IsEnabled = true;
                InputBox.IsReadOnly = true;
            }

            ReadTokens();

            if (!File.Exists(FullVersionPath)) File.Create(FullVersionPath).Close();
            else if (Convert.ToUInt16(File.ReadAllText(FullVersionPath)) != App.APP_VERSION)
            {
                SetBlur(ref InputBox, 5);
                this.Opacity = 100;
                TutBox.Visibility = System.Windows.Visibility.Visible;
                TutBox.IsEnabled = true;
                InputBox.IsReadOnly = true;
            }

            UpdateTimer.Start();
            UpdateTimer.Elapsed += (s, e) => 
            {
                GC.Collect();
                if (InputBox != null && HasBeenClicked && Started)
                    File.WriteAllText(FullTweetDraftPath, InputBox.Text.Trim());
                App.UpdateApp();
            };

            if (File.Exists(FullSettingsPath)) ReadSettings(FullSettingsPath);

            ScheduledTweetHandler.Initialize();
            ScheduledTweetHandler.SendTweets();
        }

        public static void SaveDrafts()
        {
            File.WriteAllText(FullTweetDraftsPath, JsonConvert.SerializeObject(Drafts));
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            File.WriteAllText(FullWindowPositionPath, this.Top + Environment.NewLine + this.Left);
            SaveDrafts();
            SaveSettings(FullSettingsPath);
        }

        private void ReadSettings(string fileName)
        {
            foreach (var line in File.ReadAllLines(fileName).Where(l => !l.StartsWith("#")))
            {
                var pair = line.Split('=');
                Settings[pair[0].Trim().ToLower()] = pair[1].Trim();
            }

            InputBox.FontSize = Convert.ToDouble(Settings["font-size"]);
            this.Topmost = (Settings["on-top"] == "1") ? true : false;
        }

        public static void SaveSettings(string fileName)
        {
            var sb = new StringBuilder();
            foreach(var pair in Settings)
            {
                sb.Append(pair.Key + "=" + pair.Value);
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText(fileName, sb.ToString());
        }

        private static void SetBlur(ref TextBox box, double radius)
        {
            var blurEffect = new BlurEffect();
            blurEffect.Radius = radius;
            blurEffect.KernelType = KernelType.Gaussian;
            box.Effect = blurEffect;
        }

        private static void ReadTokens()
        {
            var info = File.ReadAllLines(FullTokensPath);
            AccesToken = info[0];
            AccesSecret = info[1];
        }

        private TweetinviCore.Interfaces.oAuth.IOAuthCredentials GetCredentials()
        {
            var applicationCredentials = CredentialsCreator.GenerateApplicationCredentials(TOKEN_CONSUMER_KEY, TOKEN_CONSUMER_SECRET);
            var url = CredentialsCreator.GetAuthorizationURL(applicationCredentials);

            Process.Start(url);

            var captcha = InputDialog.Prompt();

            return CredentialsCreator.GetCredentialsFromVerifierCode(captcha, applicationCredentials);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (InputBox != null && HasBeenClicked && Started) File.WriteAllText(FullTweetDraftPath, InputBox.Text.Trim());
                File.WriteAllText(FullErrorPath, "Please DM this file to @LieuweR" + MakeNewLine(2) + "Thrown at: " + DateTime.Now.ToString() + MakeNewLine(2) + ((Exception)e.ExceptionObject).ToString());
                MessageBox.Show("Woops! See the error.txt file for more information.", "D:");
                Process.Start(FullErrorPath);
            }
            catch(Exception exp)
            {
                MessageBox.Show("Error while writing error.txt!\nMore info:\n\n" + exp.ToString() + "\n\nPlease make a screenshot of this and DM it to @LieuweR", "D:");
            }
            Environment.Exit(0);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var da = new DoubleAnimation();
                da.From = this.Opacity;
                da.To = 100;
                da.Duration = new Duration(TimeSpan.FromSeconds(100));
                if (!MessageGrid.IsEnabled && !PopUpGrid.IsEnabled) this.BeginAnimation(OpacityProperty, da);

                this.IsBeingDragged = true;
                if (System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed) this.DragMove();
                this.IsBeingDragged = false;

                if (!MessageGrid.IsEnabled && !PopUpGrid.IsEnabled) this.BeginAnimation(OpacityProperty, null);
            }

            else if (e.ChangedButton == MouseButton.Right) this.WindowState = System.Windows.WindowState.Minimized;

            else if (e.ChangedButton == MouseButton.Middle)
            {
                MainWindow_Closed(this, new EventArgs());
                Environment.Exit(0);
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!MessageGrid.IsEnabled && !PopUpGrid.IsEnabled)
            {
                var da = new DoubleAnimation();
                da.From = this.Opacity;
                da.To = this.InitialOpacity;
                da.Duration = new Duration(TimeSpan.FromSeconds(100));
                this.BeginAnimation(OpacityProperty, da);
            }
        }

        private const double ANIMATION_SPEED = .1;
        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Counter != null && InputBox != null && HasBeenClicked) this.Counter.Content = 140 - InputBox.Text.Trim().Length;

            if (InputBox.Background != null && InputBox.Text.Trim().Length <= 140 && ((SolidColorBrush)InputBox.Background).Color == Colors.Red)
            {
                this.Counter.Foreground = new SolidColorBrush(Colors.Red);
                this.Counter.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Red, Colors.Black, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));

                InputBox.Background = new SolidColorBrush(Colors.Red);
                InputBox.Foreground = new SolidColorBrush(Colors.White);

                InputBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Red, Colors.White, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
                InputBox.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.White, Colors.Black, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
            }

            else if (InputBox.Text.Trim().Length > 140 && ((SolidColorBrush)InputBox.Background).Color == Colors.White)
            {
                this.Counter.Foreground = new SolidColorBrush(Colors.Black);
                this.Counter.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Black, Colors.Red, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED + .2))));

                InputBox.Background = new SolidColorBrush(Colors.White);
                InputBox.Foreground = new SolidColorBrush(Colors.Black);

                InputBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.White, Colors.Red, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
                InputBox.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Black, Colors.White, new Duration(TimeSpan.FromSeconds(ANIMATION_SPEED))));
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            using (var client = new WebClient())
                InputBox.Text.Split(' ').Where(s => Uri.IsWellFormedUriString(s, UriKind.Absolute)).ForEach(u =>
                {
                    var shortURL = client.DownloadString("http://is.gd/create.php?format=simple&url=" + HttpUtility.UrlEncode(u));

                    if (Uri.IsWellFormedUriString(shortURL, UriKind.Absolute))
                        InputBox.Text = InputBox.Text.Replace(u, shortURL.Substring(7));
                });

            TinyTwitter.TinyTwitter.UpdateStatus(new TinyTwitter.OAuthInfo(TOKEN_CONSUMER_KEY, TOKEN_CONSUMER_SECRET, AccesToken, AccesSecret), InputBox.Text.Trim());

            InputBox.Text = "";

            InputBox.IsReadOnly = false;

            MessageGrid.Visibility = System.Windows.Visibility.Hidden;
            MessageGrid.IsEnabled = false;

            InputBox.Focus();

            ShowPopUpMessage("succesfully tjilped", Colors.Black);
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            InputBox.IsReadOnly = false;

            var da = new DoubleAnimation();
            da.From = this.Opacity;
            da.To = this.InitialOpacity;
            da.Duration = new Duration(TimeSpan.FromSeconds(.01));
            this.BeginAnimation(OpacityProperty, da);

            MessageGrid.Visibility = System.Windows.Visibility.Hidden;
            MessageGrid.IsEnabled = false;

            InputBox.Focus();
        }

        private void TutBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right)
            {
                TutBox.IsEnabled = false;
                TutBox.Visibility = System.Windows.Visibility.Hidden;
                this.Opacity = InitialOpacity;
                InputBox.IsReadOnly = false;

                InputBox.Effect.ResetBlurWithAnimation();

                if (String.IsNullOrWhiteSpace(File.ReadAllText(FullVersionPath))) ShowPopUpMessage("welcome to tjilp", Colors.Black);
                else if (Convert.ToUInt16(File.ReadAllText(FullVersionPath)) != App.APP_VERSION)
                {
                    File.WriteAllText(FullVersionPath, App.APP_VERSION.ToString());
                    ShowPopUpMessage("tjilp has been updated", Colors.Black, 2);
                }
            }
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (System.Windows.Input.Keyboard.IsKeyDown(Key.LeftShift) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightShift)) e.Handled = false;
                else
                {
                    e.Handled = true;
                    if (!String.IsNullOrWhiteSpace(InputBox.Text))
                    {
                        if (InputBox.Text.Trim().Length <= 140)
                        {
                            InputBox.IsReadOnly = true;

                            {
                                var da = new DoubleAnimation();
                                da.From = this.InitialOpacity;
                                da.To = 100;
                                da.Duration = new Duration(TimeSpan.FromSeconds(20));
                                this.BeginAnimation(OpacityProperty, da);
                            }
                            {
                                MessageGrid.Visibility = System.Windows.Visibility.Visible;
                                MessageGrid.IsEnabled = true;
                                Decline.Focus();
                                var da = new DoubleAnimation();
                                da.From = 0;
                                da.To = 100;
                                da.Duration = new Duration(TimeSpan.FromSeconds(20));
                                MessageGrid.BeginAnimation(OpacityProperty, da);
                            }
                        }
                        else
                            ShowPopUpMessage("More than 140 characters", Colors.Red);
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                TextSizeSlider.Value = InputBox.FontSize;
                TopBox.SelectedIndex = (this.Topmost) ? 0 : 1;

                SettingsPanel.Visibility = System.Windows.Visibility.Visible;
                SettingsPanel.IsEnabled = true;

                var da = new DoubleAnimation();
                da.From = SettingsPanel.Opacity;
                da.To = 100;
                da.Duration = new Duration(TimeSpan.FromSeconds(15));
                SettingsPanel.BeginAnimation(OpacityProperty, da);

                CreditsTimer.Start();
                CreditsTimer.Elapsed += CreditsTimer_Elapsed;
            }
            else if (e.Key == Key.Space)
                new System.Threading.Thread(ShortenURLs).Start();
            else if (PopUpGrid.IsEnabled) HideMessage(false);
        }

        private bool isShortening;
        private void ShortenURLs()
        {
            if (isShortening) return;

            isShortening = true;
            using (var client = new WebClient())
                Dispatcher.Invoke(new Action(() => { InputBox.Text.Split(' ').Where(s => Uri.IsWellFormedUriString(s, UriKind.Absolute)).ForEach(u => 
                {
                    var shortURL = client.DownloadString("http://is.gd/create.php?format=simple&url=" + HttpUtility.UrlEncode(u));

                    if (Uri.IsWellFormedUriString(shortURL, UriKind.Absolute))
                        InputBox.Text = InputBox.Text.Replace(u, shortURL.Substring(7));

                    InputBox.Select(InputBox.Text.Length, 0); 
                }); }));
            isShortening = false;
        }

        private static string MakeNewLine(int ammount = 1)
        {
            if (ammount == 1) return Environment.NewLine;

            var sb = new System.Text.StringBuilder();

            for(int i = 0; i < ammount; i++)
                sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        private void InputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Started = true;
            if (!HasBeenClicked)
            {
                HasBeenClicked = true;
                InputBox.Text = "";
                InputBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void InputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (HasBeenClicked && String.IsNullOrEmpty(InputBox.Text))
            {
                HasBeenClicked = false;
                InputBox.Text = RandomMessage;
                InputBox.Foreground = Brush;
            }
        }

        private void tjilpWindow_Deactivated(object sender, EventArgs e)
        {
            InputBox_LostFocus(sender, new RoutedEventArgs());
        }

        private void tjilpWindow_Activated(object sender, EventArgs e)
        {
            if(Started) InputBox_GotFocus(sender, new RoutedEventArgs());
        }

        public bool Started { get; set; }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 50) StashGrid_ShowPrevious();
            else if (e.Delta < -50) StashGrid_ShowNext();
        }

        private void StashGrid_ShowNext()
        {
            if (CurrentDraftPosition < Drafts.Count - 1) CurrentDraftPosition++;

            StashGrid_UpdateView();
        }

        private void StashGrid_ShowPrevious()
        {
            if (CurrentDraftPosition != 0) CurrentDraftPosition--;
            else
            {
                var transform = new TranslateTransform();

                var da = new DoubleAnimation();
                da.From = StashGrid.RenderTransform.Value.OffsetY;
                da.To = 190;
                da.Duration = new Duration(TimeSpan.FromSeconds(.1));
                da.FillBehavior = FillBehavior.Stop;

                da.Completed += (s, e) =>
                    {
                        StashGrid.IsEnabled = false;
                        StashGrid.Visibility = System.Windows.Visibility.Hidden;

                        StashGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(StashGrid.Opacity, 0, new Duration(TimeSpan.FromSeconds(.01))));

                        Counter.Foreground = (InputBox.Text.Trim().Length > 140) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                        Counter.Content = (Started && HasBeenClicked) ? (140 - InputBox.Text.Trim().Length).ToString() : "140";
                    };

                transform.BeginAnimation(TranslateTransform.YProperty, da);

                StashGrid.RenderTransform = transform;

                CurrentDraftPosition = 0;

                SaveDrafts();
            }

            StashGrid_UpdateView();
        }

        private void StashGrid_UpdateView()
        {
            if (Drafts.Count == 0)
            {
                Counter.Foreground = (InputBox.Text.Trim().Length > 140) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                Counter.Content = (Started && HasBeenClicked) ? (140 - InputBox.Text.Trim().Length).ToString() : "140";

                StashGrid.IsEnabled = false;
                StashGrid.Visibility = System.Windows.Visibility.Hidden;

                var da = new DoubleAnimation();
                da.From = StashGrid.Opacity;
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromSeconds(.01));
                StashGrid.BeginAnimation(OpacityProperty, da);

                SaveDrafts();
            }
            else
            {
                try
                {
                    TweetLabel.Text = "\"" + Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).Tweet + "\"";
                    DateLabel.Content = Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).DateTime.ToString("yyy-MM-dd");

                    Counter.Foreground = new SolidColorBrush(Colors.Black);
                    Counter.Content = (CurrentDraftPosition + 1) + "/" + Drafts.Count;
                }
                catch(ArgumentOutOfRangeException)
                {
                    CurrentDraftPosition = 0;

                    TweetLabel.Text = "\"" + Drafts.Reverse<TweetDraft>().First().Tweet + "\"";
                    DateLabel.Content = Drafts.Reverse<TweetDraft>().First().DateTime.ToString("yyy-MM-dd");

                    Counter.Foreground = new SolidColorBrush(Colors.Black);
                    Counter.Content = "1/" + Drafts.Count;
                }
            }
        }

        private void TweetLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            InputBox.Foreground = new SolidColorBrush(Colors.Black);
            Started = HasBeenClicked = true;
            InputBox.Text = Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).Tweet;
            InputBox.Select(InputBox.Text.Length, 0);

            StashGrid.IsEnabled = false;
            StashGrid.Visibility = System.Windows.Visibility.Hidden;

            var da = new DoubleAnimation();
            da.From = StashGrid.Opacity;
            da.To = 0;
            da.Duration = new Duration(TimeSpan.FromSeconds(.01));
            StashGrid.BeginAnimation(OpacityProperty, da);

            SaveDrafts();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Drafts.RemoveAt((Drafts.Count - 1) - CurrentDraftPosition);

                SaveDrafts();
            }
            catch (ArgumentOutOfRangeException) 
            {
                if(Drafts.Count == 1)
                {
                    Drafts.RemoveAt(0);
                    SaveDrafts();
                }
            }
            finally
            {
                StashGrid_UpdateView();
            }
        }

        private void InputBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (!String.IsNullOrWhiteSpace(InputBox.Text) && Started && HasBeenClicked)
                {
                    Drafts.Add(new TweetDraft()
                    {
                        Tweet = InputBox.Text.Trim(),
                        DateTime = DateTime.Now
                    });

                    InputBox.Text = "";

                    SaveDrafts();
                }
            }

            if (e.Delta < -50 && Drafts.Count != 0)
            {
                CurrentDraftPosition = 0;
                StashGrid_UpdateView();

                StashGrid.IsEnabled = true;
                StashGrid.Visibility = System.Windows.Visibility.Visible;

                StashGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(StashGrid.Opacity, 100, new Duration(TimeSpan.FromMilliseconds(0.1))));
                var transform = new TranslateTransform();

                var da = new DoubleAnimation();
                da.From = 190;
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromSeconds(.1));
                da.FillBehavior = FillBehavior.Stop;

                transform.BeginAnimation(TranslateTransform.YProperty, da);

                StashGrid.RenderTransform = transform;
            }
        }

        private void DateLabel_MouseEnter(object sender, MouseEventArgs e)
        {
            DateLabel.BeginAnimation(FontSizeProperty, new DoubleAnimation(23, 19, new Duration(TimeSpan.FromSeconds(.05))));

            DateLabel.Content = "click to schedule";
        }

        private void DateLabel_MouseLeave(object sender, MouseEventArgs e)
        {
            DateLabel.BeginAnimation(FontSizeProperty, new DoubleAnimation(19, 23, new Duration(TimeSpan.FromSeconds(.05))));

            StashGrid_UpdateView();
        }
    }

    public static class Extensions
    {
        public static void ResetBlurWithAnimation(this Effect effect)
        {
            var blurEffect = effect as BlurEffect;

            if (blurEffect == null) throw new ArgumentException("Argument has to be BlurEffect.");

            var da = new DoubleAnimation();
            da.From = blurEffect.Radius;
            da.To = 0;
            da.Duration = new Duration(TimeSpan.FromSeconds(.25));
            effect.BeginAnimation(BlurEffect.RadiusProperty, da);
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null || action == null) throw new ArgumentNullException();
            foreach (T item in collection) action(item);
        }
    }
}
