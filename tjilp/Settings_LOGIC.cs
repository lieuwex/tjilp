using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace tjilp
{
    public partial class MainWindow : Window
    {
        public static readonly Dictionary<string, string> Credits = new Dictionary<string, string>
        {
            {"by @LieuweR", "http://www.twitter.com/LieuweR"},
            {"Thanks to @Jelster64", "http://www.twitter.com/jelster64"},
            {"Thanks to @dwardcraft", "http://www.twitter.com/dwardcraft"},
            {"Thanks to @Joost_Marcus", "http://twitter.com/Joost_marcus"}
        };
        static int CurrentCreditsPosition;
        static readonly Timer CreditsTimer = new Timer(2300);

        void CreditsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CreditsTimer.Stop();
            this.Dispatcher.Invoke(() =>
            {
                var da = new DoubleAnimation();
                da.From = this.CreditsLabel.Opacity;
                da.To = 0;
                da.Duration = new Duration(TimeSpan.FromSeconds(.75));
                da.Completed += (s, x) =>
                {
                    if (CurrentCreditsPosition != Credits.Count - 1) CurrentCreditsPosition++;
                    else CurrentCreditsPosition = 0;
                    this.CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;
                    this.CreditsLabel.BeginAnimation(OpacityProperty, null);
                    CreditsTimer.Start();
                };
                this.CreditsLabel.BeginAnimation(OpacityProperty, da);
            });
        }

        void SettingsPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.SettingsPanel.Visibility = Visibility.Hidden;
                this.SettingsPanel.IsEnabled = false;
                this.SettingsPanel.Opacity = 0;
                this.SettingsPanel.BeginAnimation(OpacityProperty, null);
                this.CreditsLabel.BeginAnimation(OpacityProperty, null);
                CreditsTimer.Stop();
                CurrentCreditsPosition = 0;
                this.CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;
            }
        }

        void SettingsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SettingsPanel.Visibility = Visibility.Hidden;
            this.SettingsPanel.IsEnabled = false;
            this.SettingsPanel.Opacity = 0;
            this.SettingsPanel.BeginAnimation(OpacityProperty, null);
            this.CreditsLabel.BeginAnimation(OpacityProperty, null);
            CreditsTimer.Stop();
            CurrentCreditsPosition = 0;
            this.CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;
        }

        void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SettingsPanel.Visibility = Visibility.Hidden;
            this.SettingsPanel.IsEnabled = false;
            this.SettingsPanel.Opacity = 0;
            this.SettingsPanel.BeginAnimation(OpacityProperty, null);
            this.CreditsLabel.BeginAnimation(OpacityProperty, null);
            CreditsTimer.Stop();
            CurrentCreditsPosition = 0;
            this.CreditsLabel.Content = Credits.ElementAt(CurrentCreditsPosition).Key;
        }

        void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { Process.Start(Credits.ElementAt(CurrentCreditsPosition).Value); }

        void TextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.InputBox.FontSize = this.TextSizeSlider.Value;
            this.Settings.FontSize = this.TextSizeSlider.Value;
        }

        void TextSizeSlider_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.InputBox.FontSize = 22;
            this.TextSizeSlider.Value = 22;
            this.Settings.FontSize = 22;
            var t = new Timer(.1);
            t.Start();
            t.Elapsed += (s, x) =>
            {
                t.Stop();
                this.Dispatcher.Invoke(() => { this.WindowState = WindowState.Normal; });
            };
        }

        void TopBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var onTop = this.TopBox.SelectedIndex == 0;
            this.Settings.OnTop = onTop;
            this.Topmost = onTop;
        }
    }
}