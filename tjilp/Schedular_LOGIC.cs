using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace tjilp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int CurrentSchedularPosition;

        void DateLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var slide1Transform = new TranslateTransform();
            slide1Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.Slide1Grid.RenderTransform.Value.OffsetY, 0, new Duration(TimeSpan.FromSeconds(.01))));
            this.Slide1Grid.RenderTransform = slide1Transform;
            var slide2Transform = new TranslateTransform();
            slide2Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.Slide2Grid.RenderTransform.Value.OffsetY, 200, new Duration(TimeSpan.FromSeconds(.01))));
            this.Slide2Grid.RenderTransform = slide2Transform;
            var timeTransform = new TranslateTransform();
            timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.TimeGrid.RenderTransform.Value.OffsetY, 600, new Duration(TimeSpan.FromSeconds(.01))));
            this.TimeGrid.RenderTransform = timeTransform;
            var dayOfWeekTransform = new TranslateTransform();
            dayOfWeekTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.DayOfWeekGrid.RenderTransform.Value.OffsetY, 400, new Duration(TimeSpan.FromSeconds(.01))));
            this.DayOfWeekGrid.RenderTransform = dayOfWeekTransform;
            var dateTransform = new TranslateTransform();
            dateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.DateGrid.RenderTransform.Value.OffsetY, 400, new Duration(TimeSpan.FromSeconds(.01))));
            this.DateGrid.RenderTransform = dateTransform;
            this.CurrentSchedularPosition = 0;
            var current = Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).ScheduledTweet;
            if (current != null)
            {
                try
                {
                    this.NameInput.Text = current.Name;
                    if (current.Dates.Count > 1)
                    {
                        this.DatePicker.SelectionMode = CalendarSelectionMode.MultipleRange;
                        current.Dates.ForEach(d => this.DatePicker.SelectedDates.Add(d));
                    }
                    else
                        this.DatePicker.SelectedDate = current.Dates.Single();
                    this.RepeatModeBox.SelectedIndex = (int)current.RepeatMode;
                    this.HourBox.Text = current.Time.ToString("HH");
                    this.MinuteBox.Text = current.Time.ToString("mm");
                    this.MondayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Monday);
                    this.TuesdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Tuesday);
                    this.WednesdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Wednesday);
                    this.ThursdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Thursday);
                    this.FridayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Friday);
                    this.SaturdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Saturday);
                    this.SundayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Sunday);
                }
                catch {}
            }
            else
            {
                this.DatePicker.SelectedDate = DateTime.Today;
                this.HourBox.Text = DateTime.Now.ToString("HH");
                this.MinuteBox.Text = DateTime.Now.ToString("mm");
            }
            this.SchedularGrid.IsEnabled = true;
            this.SchedularGrid.Visibility = Visibility.Visible;
            var da = new DoubleAnimation();
            da.From = 0;
            da.To = 100;
            da.Duration = new Duration(TimeSpan.FromSeconds(20));
            this.SchedularGrid.BeginAnimation(OpacityProperty, da);
            this.StashGrid.Effect = new BlurEffect();
            this.StashGrid.Effect.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(0, 5, new Duration(TimeSpan.FromSeconds(.2))));
            this.NameInput.Focus();
            this.NameInput.SelectAll();
        }

        void hideSchedular(bool save)
        {
            this.SchedularGrid.IsEnabled = false;
            this.SchedularGrid.Visibility = Visibility.Hidden;
            this.SchedularGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(100, 0, new Duration(TimeSpan.FromMilliseconds(.1))));
            this.StashGrid.Effect.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(5, 0, new Duration(TimeSpan.FromSeconds(.001))));
            if (save)
            {
                var tmpDaysOfWeek = new List<DayOfWeek>();
                if ((bool)this.MondayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Monday);
                if ((bool)this.TuesdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Tuesday);
                if ((bool)this.WednesdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Wednesday);
                if ((bool)this.ThursdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Thursday);
                if ((bool)this.FridayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Friday);
                if ((bool)this.SaturdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Saturday);
                if ((bool)this.SundayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Sunday);
                Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).ScheduledTweet = new ScheduledTweet
                {
                    Name = this.NameInput.Text.Trim(),
                    RepeatMode = (RepeatMode)this.RepeatModeBox.SelectedIndex,
                    Dates = this.DatePicker.SelectedDates.ToList(),
                    Time = DateTime.Parse(this.HourBox.Text.Trim() + ":" + this.MinuteBox.Text.Trim()),
                    DaysOfWeek = tmpDaysOfWeek
                };
                SaveDrafts();
            }
            this.NameInput.Text = "name.";
            this.RepeatModeBox.SelectedIndex = 0;
            this.DatePicker.SelectionMode = CalendarSelectionMode.SingleDate;
            this.DatePicker.SelectedDate = DateTime.Today;
            this.MondayCheck.IsChecked = false;
            this.TuesdayCheck.IsChecked = false;
            this.WednesdayCheck.IsChecked = false;
            this.ThursdayCheck.IsChecked = false;
            this.FridayCheck.IsChecked = false;
            this.SaturdayCheck.IsChecked = false;
            this.SundayCheck.IsChecked = false;
        }

        void SchedularGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < -50) this.Schedular_MoveDown();
            else if (e.Delta > 50) this.Schedular_MoveUp();
        }

        void Schedular_MoveDown()
        {
            if ((this.CurrentSchedularPosition == 3 && (RepeatMode)this.RepeatModeBox.SelectedIndex != RepeatMode.Time) || (this.CurrentSchedularPosition == 2 && (RepeatMode)this.RepeatModeBox.SelectedIndex == RepeatMode.Time))
                this.hideSchedular(true);
            this.CurrentSchedularPosition++;

            var slide1Transform = new TranslateTransform();
            slide1Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.Slide1Grid.RenderTransform.Value.OffsetY, Math.Round((this.Slide1Grid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.Slide1Grid.RenderTransform = slide1Transform;

            var slide2Transform = new TranslateTransform();
            slide2Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.Slide2Grid.RenderTransform.Value.OffsetY, Math.Round((this.Slide2Grid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.Slide2Grid.RenderTransform = slide2Transform;

            var timeTransform = new TranslateTransform();
            timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.TimeGrid.RenderTransform.Value.OffsetY, Math.Round((this.TimeGrid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.TimeGrid.RenderTransform = timeTransform;

            var dayOfWeekTransform = new TranslateTransform();
            dayOfWeekTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.DayOfWeekGrid.RenderTransform.Value.OffsetY, Math.Round((this.DayOfWeekGrid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.DayOfWeekGrid.RenderTransform = dayOfWeekTransform;

            var dateTransform = new TranslateTransform();
            dateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.DateGrid.RenderTransform.Value.OffsetY, Math.Round((this.DateGrid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.DateGrid.RenderTransform = dateTransform;
        }

        void Schedular_MoveUp()
        {
            if (this.CurrentSchedularPosition == 0)
            {
                this.hideSchedular(false);
                return;
            }
            this.CurrentSchedularPosition--;

            var slide1Transform = new TranslateTransform();
            slide1Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.Slide1Grid.RenderTransform.Value.OffsetY, Math.Round((this.Slide1Grid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.Slide1Grid.RenderTransform = slide1Transform;

            var slide2Transform = new TranslateTransform();
            slide2Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.Slide2Grid.RenderTransform.Value.OffsetY, Math.Round((this.Slide2Grid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.Slide2Grid.RenderTransform = slide2Transform;

            var timeTransform = new TranslateTransform();
            timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.TimeGrid.RenderTransform.Value.OffsetY, Math.Round((this.TimeGrid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.TimeGrid.RenderTransform = timeTransform;

            var dateTransform = new TranslateTransform();
            dateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.DateGrid.RenderTransform.Value.OffsetY, Math.Round((this.DateGrid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.DateGrid.RenderTransform = dateTransform;

            var dayOfWeekTransform = new TranslateTransform();
            dayOfWeekTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.DayOfWeekGrid.RenderTransform.Value.OffsetY, Math.Round((this.DayOfWeekGrid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            this.DayOfWeekGrid.RenderTransform = dayOfWeekTransform;
        }

        void RepeatModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DatePicker != null && this.RepeatModeBox != null)
                this.DatePicker.SelectionMode = ((RepeatMode)this.RepeatModeBox.SelectedIndex == RepeatMode.Date) ? CalendarSelectionMode.MultipleRange : CalendarSelectionMode.SingleDate;
            if (this.DayOfWeekGrid != null && this.RepeatModeBox != null)
            {
                this.DayOfWeekGrid.IsEnabled = (RepeatMode)this.RepeatModeBox.SelectedIndex == RepeatMode.DayOfWeek;
                this.DayOfWeekGrid.Visibility = (this.DayOfWeekGrid.IsEnabled) ? Visibility.Visible : Visibility.Hidden;
            }
            if (this.DateGrid != null && this.RepeatModeBox != null && this.TimeGrid != null)
            {
                this.DateGrid.IsEnabled = (RepeatMode)this.RepeatModeBox.SelectedIndex != RepeatMode.Time || (RepeatMode)this.RepeatModeBox.SelectedIndex != RepeatMode.DayOfWeek;
                this.DateGrid.Visibility = ((RepeatMode)this.RepeatModeBox.SelectedIndex != RepeatMode.Time && !this.DayOfWeekGrid.IsEnabled) ? Visibility.Visible : Visibility.Hidden;
                if ((RepeatMode)this.RepeatModeBox.SelectedIndex == RepeatMode.Time)
                {
                    var timeTransform = new TranslateTransform();
                    timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.TimeGrid.RenderTransform.Value.OffsetY, this.DateGrid.RenderTransform.Value.OffsetY, new Duration(TimeSpan.FromSeconds(.001))));
                    this.TimeGrid.RenderTransform = timeTransform;
                }
                if ((RepeatMode)this.RepeatModeBox.SelectedIndex != RepeatMode.Time && this.TimeGrid.RenderTransform.Value.OffsetY == this.DateGrid.RenderTransform.Value.OffsetY)
                {
                    var timeTransform = new TranslateTransform();
                    timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(this.TimeGrid.RenderTransform.Value.OffsetY, this.DateGrid.RenderTransform.Value.OffsetY + 200, new Duration(TimeSpan.FromSeconds(.001))));
                    this.TimeGrid.RenderTransform = timeTransform;
                }
            }
        }

        void HourOrMinuteBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (Convert.ToInt32(this.HourBox.Text) > 23)
                {
                    e.Handled = true;
                    this.HourBox.Text = "00";
                    this.HourBox.Focus();
                    this.HourBox.SelectAll();
                }
                if (Convert.ToInt32(this.MinuteBox.Text) > 59)
                {
                    e.Handled = true;
                    this.MinuteBox.Text = "00";
                    this.MinuteBox.Focus();
                    this.MinuteBox.SelectAll();
                }
            }
            catch (FormatException) {}
            catch (OverflowException) {}
            if (!char.IsDigit(e.Text, e.Text.Length - 1) || e.Text.Length >= 2) e.Handled = true;
        }
    }
}