using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Tweetinvi;

namespace tjilp
{
    public partial class MainWindow : Window
    {
        public static readonly Dictionary<string, string> Credits = new Dictionary<string, string>()
        {
            {"by @LieuweR", "http://www.twitter.com/LieuweR"},
            {"Thanks to @Jelster64", "http://www.twitter.com/jelster64"},
            {"Thanks to @dwardcraft", "http://www.twitter.com/dwardcraft"},
            {"Thanks to @Joost_Marcus", "http://twitter.com/Joost_marcus"}
        };

        private static int CurrentCreditsPosition = 0;

        private static Timer CreditsTimer = new Timer(3500);

        void CreditsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CreditsTimer.Stop();

            Dispatcher.Invoke(new Action(() =>
            {
                var da = new DoubleAnimation();
                da.From = CreditsLabel.Opacity;
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromSeconds(1));
                da.Completed += (s, x) =>
                {
                    if (CurrentCreditsPosition != Credits.Count - 1) CurrentCreditsPosition++;
                    else CurrentCreditsPosition = 0;

                    CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;

                    CreditsLabel.BeginAnimation(OpacityProperty, null);
                    CreditsTimer.Start();
                };
                CreditsLabel.BeginAnimation(OpacityProperty, da);
            }));
        }

        private void SettingsPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SettingsPanel.Visibility = System.Windows.Visibility.Hidden;
                SettingsPanel.IsEnabled = false;
                SettingsPanel.Opacity = 0;
                SettingsPanel.BeginAnimation(OpacityProperty, null);

                CreditsLabel.BeginAnimation(OpacityProperty, null);
                CreditsTimer.Stop();
                CurrentCreditsPosition = 0;
                CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;

                SaveSettings(FullSettingsPath);
            }
        }
        private void SettingsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SettingsPanel.Visibility = System.Windows.Visibility.Hidden;
            SettingsPanel.IsEnabled = false;
            SettingsPanel.Opacity = 0;
            SettingsPanel.BeginAnimation(OpacityProperty, null);

            CreditsLabel.BeginAnimation(OpacityProperty, null);
            CreditsTimer.Stop();
            CurrentCreditsPosition = 0;
            CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;

            SaveSettings(FullSettingsPath);
        }

        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SettingsPanel.Visibility = System.Windows.Visibility.Hidden;
            SettingsPanel.IsEnabled = false;
            SettingsPanel.Opacity = 0;
            SettingsPanel.BeginAnimation(OpacityProperty, null);

            CreditsLabel.BeginAnimation(OpacityProperty, null);
            CreditsTimer.Stop();
            CurrentCreditsPosition = 0;
            CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;

            SaveSettings(FullSettingsPath);
        }

        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(Credits.ElementAt(CurrentCreditsPosition).Value);
        }

        private void TextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            InputBox.FontSize = TextSizeSlider.Value;
            Settings["font-size"] = TextSizeSlider.Value.ToString();
        }

        private void TextSizeSlider_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            InputBox.FontSize = 22;
            TextSizeSlider.Value = 22;
            Settings["font-size"] = "22";

            var t = new Timer(.1);
            t.Start();
            t.Elapsed += (s, x) => { t.Stop(); Dispatcher.Invoke(new Action(() => { this.WindowState = System.Windows.WindowState.Normal; })); };
        }

        private void TopBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var onTop = TopBox.SelectedIndex == 0;
            Settings["on-top"] = (onTop) ? "1" : "0";
            this.Topmost = onTop;
        }
    }
}
