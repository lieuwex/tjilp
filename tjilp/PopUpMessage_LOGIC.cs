using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace tjilp
{
    public partial class MainWindow
    {
        TaskCompletionSource<bool> Tcs;
        Timer Timer;

        void initializeTimer(double seconds)
        {
            if (this.Timer != null) this.Timer.Elapsed -= this.t_Elapsed;
            this.Timer = new Timer(seconds * 1000);
            this.Timer.Elapsed += this.t_Elapsed;
            this.Timer.Start();
        }

        void hideMessage(bool enableAnimation)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                if (!this.PopUpGrid.IsEnabled) return;
                this.Timer.Stop();
                if (enableAnimation)
                {
                    if (!this.IsBeingDragged)
                    {
                        var da = new DoubleAnimation
                        {
                            From = this.Opacity,
                            To = this.InitialOpacity,
                            Duration = new Duration(TimeSpan.FromSeconds(1.001))
                        };
                        this.BeginAnimation(OpacityProperty, da);
                    }
                    var animation = new DoubleAnimation
                    {
                        From = 100,
                        To = 0,
                        Duration = new Duration(TimeSpan.FromSeconds(1))
                    };
                    animation.Completed += (s, e) => this.Dispatcher.Invoke(delegate
                    {
                        this.PopUpGrid.Visibility = Visibility.Hidden;
                        this.PopUpGrid.IsEnabled = false;
                        this.Opacity = this.InitialOpacity;
                        if (!this.Tcs.Task.IsCompleted) this.Tcs.SetResult(true);
                    });
                    this.PopUpGrid.BeginAnimation(OpacityProperty, animation);
                }
                else
                {
                    if (!this.IsBeingDragged)
                    {
                        var da = new DoubleAnimation
                        {
                            From = this.Opacity,
                            To = this.InitialOpacity,
                            Duration = new Duration(TimeSpan.FromSeconds(0.001))
                        };
                        this.BeginAnimation(OpacityProperty, da);
                        da = new DoubleAnimation
                        {
                            From = 100,
                            To = 0,
                            Duration = new Duration(TimeSpan.FromSeconds(0.001))
                        };
                        this.PopUpGrid.BeginAnimation(OpacityProperty, da);
                    }
                    this.PopUpGrid.Visibility = Visibility.Hidden;
                    this.PopUpGrid.IsEnabled = false;
                    this.Opacity = this.InitialOpacity;
                    if (!this.Tcs.Task.IsCompleted) this.Tcs.SetResult(true);
                }
            });
        }

        void t_Elapsed(object sender, ElapsedEventArgs e) { this.hideMessage(true); }

        public async Task ShowPopUpMessage(string message, Color color, double secondsToShow = 1.5)
        {
            if (this.Tcs != null && !this.Tcs.Task.IsCompleted)
            {
                this.initializeTimer(secondsToShow);
                return;
            }
            this.Tcs = new TaskCompletionSource<bool>();

            this.Message.Content = message;
            this.Message.Foreground = new SolidColorBrush(color);

            this.BeginAnimation(OpacityProperty, new DoubleAnimation(this.Opacity, 100, new Duration(TimeSpan.FromSeconds(20))));

            this.PopUpGrid.Visibility = Visibility.Visible;
            this.PopUpGrid.IsEnabled = true;
            this.PopUpGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 100, new Duration(TimeSpan.FromSeconds(20))));

            this.initializeTimer(secondsToShow);
            await this.Tcs.Task;
        }

        void PopUpGrid_MouseDown(object sender, MouseButtonEventArgs e) { this.hideMessage(false); }
        void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (this.PopUpGrid.IsEnabled) this.hideMessage(false); }
    }
}