using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace tjilp
{
    public partial class MainWindow : Window
    {
        private Timer Timer = new Timer();

        private void InitializeTimer(double ms)
        {
            Timer.Elapsed += t_Elapsed;
            Timer.Interval = ms;
        }

        private void HideMessage(bool enableAnimation)
        {
            Dispatcher.Invoke(() =>
            {
                if (!PopUpGrid.IsEnabled) return;

                Timer.Stop();
                if (enableAnimation)
                {
                    if (!this.IsBeingDragged)
                    {
                        var da = new DoubleAnimation();
                        da.From = this.Opacity;
                        da.To = this.InitialOpacity;
                        da.Duration = new Duration(TimeSpan.FromSeconds(1.001));
                        this.BeginAnimation(OpacityProperty, da);
                    }

                    var animation = new DoubleAnimation();
                    animation.From = PopUpGrid.Opacity;
                    animation.To = 0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                    PopUpGrid.BeginAnimation(OpacityProperty, animation);

                    var t = new Timer();
                    t.Interval = 1000;
                    t.Start();
                    t.Elapsed += (s, e) =>
                    {
                        Dispatcher.InvokeAsync(new Action(() =>
                        {
                            PopUpGrid.Visibility = System.Windows.Visibility.Hidden;
                            PopUpGrid.IsEnabled = false;

                            this.Opacity = this.InitialOpacity;
                        }));
                        t.Stop();
                    };
                }
                else
                {
                    if (!this.IsBeingDragged)
                    {
                        var da = new DoubleAnimation();
                        da.From = this.Opacity;
                        da.To = this.InitialOpacity;
                        da.Duration = new Duration(TimeSpan.FromSeconds(0.001));
                        this.BeginAnimation(OpacityProperty, da);

                        da = new DoubleAnimation();
                        da.From = PopUpGrid.Opacity;
                        da.To = 0;
                        da.Duration = new Duration(TimeSpan.FromSeconds(0.001));
                        PopUpGrid.BeginAnimation(OpacityProperty, da);
                    }

                    PopUpGrid.Visibility = System.Windows.Visibility.Hidden;
                    PopUpGrid.IsEnabled = false;

                    this.Opacity = this.InitialOpacity;
                }
            });
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.HideMessage(true);
        }

        public void ShowPopUpMessage(string message, Color color, double secondsToShow = 1.5)
        {
            if (PopUpGrid.IsEnabled) { Timer.Stop(); Timer.Start(); return; }

            Message.Content = message;
            Message.Foreground = new SolidColorBrush(color);

            InitializeTimer(secondsToShow * 1000);

            {
                var da = new DoubleAnimation();
                da.From = this.Opacity;
                da.To = 100;
                da.Duration = new Duration(TimeSpan.FromSeconds(20));
                this.BeginAnimation(OpacityProperty, da);
            }
            {
                PopUpGrid.Visibility = System.Windows.Visibility.Visible;
                PopUpGrid.IsEnabled = true;
                var da = new DoubleAnimation();
                da.From = PopUpGrid.Opacity;
                da.To = 100;
                da.Duration = new Duration(TimeSpan.FromSeconds(20));
                PopUpGrid.BeginAnimation(OpacityProperty, da);
            }

            Timer.Start();
        }

        private void PopUpGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.HideMessage(false);
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PopUpGrid.IsEnabled) HideMessage(false);
        }
    }
}
