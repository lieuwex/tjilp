using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace tjilp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int CurrentSchedularPosition = 0;

        private void DateLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var slide1Transform = new TranslateTransform();

            slide1Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(Slide1Grid.RenderTransform.Value.OffsetY, 0, new Duration(TimeSpan.FromSeconds(.01))));
            Slide1Grid.RenderTransform = slide1Transform;

            var slide2Transform = new TranslateTransform();

            slide2Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(Slide2Grid.RenderTransform.Value.OffsetY, 200, new Duration(TimeSpan.FromSeconds(.01))));
            Slide2Grid.RenderTransform = slide2Transform;

            var timeTransform = new TranslateTransform();

            timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(TimeGrid.RenderTransform.Value.OffsetY, 600, new Duration(TimeSpan.FromSeconds(.01))));
            TimeGrid.RenderTransform = timeTransform;

            var dayOfWeekTransform = new TranslateTransform();

            dayOfWeekTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(DayOfWeekGrid.RenderTransform.Value.OffsetY, 400, new Duration(TimeSpan.FromSeconds(.01))));
            DayOfWeekGrid.RenderTransform = dayOfWeekTransform;

            var dateTransform = new TranslateTransform();

            dateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(DateGrid.RenderTransform.Value.OffsetY, 400, new Duration(TimeSpan.FromSeconds(.01))));
            DateGrid.RenderTransform = dateTransform;

            CurrentSchedularPosition = 0;

            var current = Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).ScheduledTweet;
            if (current != null)
            {
                try
                {
                    NameInput.Text = current.Name;
                    if (current.Dates.Count > 1) { DatePicker.SelectionMode = CalendarSelectionMode.MultipleRange; current.Dates.ForEach(d => DatePicker.SelectedDates.Add(d)); }
                    else { DatePicker.SelectedDate = current.Dates.Single(); }
                    RepeatModeBox.SelectedIndex = (int)current.RepeatMode;

                    HourBox.Text = current.Time.ToString("HH");
                    MinuteBox.Text = current.Time.ToString("mm");

                    MondayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Monday);
                    TuesdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Tuesday);
                    WednesdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Wednesday);
                    ThursdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Thursday);
                    FridayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Friday);
                    SaturdayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Saturday);
                    SundayCheck.IsChecked = current.DaysOfWeek.Contains(DayOfWeek.Sunday);
                }
                catch { }
            }
            else
            {
                DatePicker.SelectedDate = DateTime.Today;

                HourBox.Text = DateTime.Now.ToString("HH");
                MinuteBox.Text = DateTime.Now.ToString("mm");
            }

            SchedularGrid.IsEnabled = true;
            SchedularGrid.Visibility = System.Windows.Visibility.Visible;

            var da = new DoubleAnimation();
            da.From = 0;
            da.To = 100;
            da.Duration = new Duration(TimeSpan.FromSeconds(20));
            SchedularGrid.BeginAnimation(OpacityProperty, da);

            StashGrid.Effect = new BlurEffect();
            StashGrid.Effect.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(0, 5, new Duration(TimeSpan.FromSeconds(.2))));

            NameInput.Focus();
            NameInput.SelectAll();
        }

        private void HideSchedular(bool save)
        {
            SchedularGrid.IsEnabled = false;
            SchedularGrid.Visibility = System.Windows.Visibility.Hidden;

            SchedularGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(100, 0, new Duration(TimeSpan.FromMilliseconds(.1))));

            StashGrid.Effect.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(5, 0, new Duration(TimeSpan.FromSeconds(.001))));

            if (save)
            {
                var tmpDaysOfWeek = new List<DayOfWeek>();
                if ((bool)MondayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Monday);
                if ((bool)TuesdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Tuesday);
                if ((bool)WednesdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Wednesday);
                if ((bool)ThursdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Thursday);
                if ((bool)FridayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Friday);
                if ((bool)SaturdayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Saturday);
                if ((bool)SundayCheck.IsChecked) tmpDaysOfWeek.Add(DayOfWeek.Sunday);

                Drafts.Reverse<TweetDraft>().ElementAt(CurrentDraftPosition).ScheduledTweet = new ScheduledTweet()
                {
                    Name = NameInput.Text.Trim(),
                    RepeatMode = (RepeatMode)RepeatModeBox.SelectedIndex,
                    Dates = DatePicker.SelectedDates.ToList(),
                    Time = DateTime.Parse(HourBox.Text.Trim() + ":" + MinuteBox.Text.Trim()),
                    DaysOfWeek = tmpDaysOfWeek
                };
                new Thread(SaveDrafts).Start();
            }

            NameInput.Text = "name.";
            RepeatModeBox.SelectedIndex = 0;
            DatePicker.SelectionMode = CalendarSelectionMode.SingleDate;
            DatePicker.SelectedDate = DateTime.Today;

            MondayCheck.IsChecked = false;
            TuesdayCheck.IsChecked = false;
            WednesdayCheck.IsChecked = false;
            ThursdayCheck.IsChecked = false;
            FridayCheck.IsChecked = false;
            SaturdayCheck.IsChecked = false;
            SundayCheck.IsChecked = false;
        }

        private void SchedularGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < -50) Schedular_MoveDown();
            else if (e.Delta > 50) Schedular_MoveUp();
        }

        private void Schedular_MoveDown()
        {
            if ((CurrentSchedularPosition == 3 && (RepeatMode)RepeatModeBox.SelectedIndex != RepeatMode.Time) ||
                (CurrentSchedularPosition == 2 && (RepeatMode)RepeatModeBox.SelectedIndex == RepeatMode.Time))
                HideSchedular(true);

            CurrentSchedularPosition++;

            var slide1Transform = new TranslateTransform();

            slide1Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(Slide1Grid.RenderTransform.Value.OffsetY, Math.Round((Slide1Grid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            Slide1Grid.RenderTransform = slide1Transform;

            var slide2Transform = new TranslateTransform();

            slide2Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(Slide2Grid.RenderTransform.Value.OffsetY, Math.Round((Slide2Grid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            Slide2Grid.RenderTransform = slide2Transform;

            var timeTransform = new TranslateTransform();

            timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(TimeGrid.RenderTransform.Value.OffsetY, Math.Round((TimeGrid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            TimeGrid.RenderTransform = timeTransform;

            var dayOfWeekTransform = new TranslateTransform();

            dayOfWeekTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(DayOfWeekGrid.RenderTransform.Value.OffsetY, Math.Round((DayOfWeekGrid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            DayOfWeekGrid.RenderTransform = dayOfWeekTransform;

            var dateTransform = new TranslateTransform();

            dateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(DateGrid.RenderTransform.Value.OffsetY, Math.Round((DateGrid.RenderTransform.Value.OffsetY - 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            DateGrid.RenderTransform = dateTransform;
        }

        private void Schedular_MoveUp()
        {
            if (CurrentSchedularPosition == 0) { HideSchedular(false); return; }

            CurrentSchedularPosition--;

            var slide1Transform = new TranslateTransform();

            slide1Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(Slide1Grid.RenderTransform.Value.OffsetY, Math.Round((Slide1Grid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            Slide1Grid.RenderTransform = slide1Transform;

            var slide2Transform = new TranslateTransform();

            slide2Transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(Slide2Grid.RenderTransform.Value.OffsetY, Math.Round((Slide2Grid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            Slide2Grid.RenderTransform = slide2Transform;

            var timeTransform = new TranslateTransform();

            timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(TimeGrid.RenderTransform.Value.OffsetY, Math.Round((TimeGrid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            TimeGrid.RenderTransform = timeTransform;

            var dateTransform = new TranslateTransform();

            dateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(DateGrid.RenderTransform.Value.OffsetY, Math.Round((DateGrid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            DateGrid.RenderTransform = dateTransform;

            var dayOfWeekTransform = new TranslateTransform();

            dayOfWeekTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(DayOfWeekGrid.RenderTransform.Value.OffsetY, Math.Round((DayOfWeekGrid.RenderTransform.Value.OffsetY + 200) / 200) * 200, new Duration(TimeSpan.FromSeconds(.2))));
            DayOfWeekGrid.RenderTransform = dayOfWeekTransform;
        }

        private void RepeatModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker != null && RepeatModeBox != null) DatePicker.SelectionMode = ((RepeatMode)RepeatModeBox.SelectedIndex == RepeatMode.Date) ? CalendarSelectionMode.MultipleRange : CalendarSelectionMode.SingleDate;
            if (DayOfWeekGrid != null && RepeatModeBox != null)
            {
                DayOfWeekGrid.IsEnabled = (RepeatMode)RepeatModeBox.SelectedIndex == RepeatMode.DayOfWeek;
                DayOfWeekGrid.Visibility = (DayOfWeekGrid.IsEnabled) ? Visibility.Visible : Visibility.Hidden;
            }

            if (DateGrid != null && RepeatModeBox != null && TimeGrid != null)
            {
                DateGrid.IsEnabled = (RepeatMode)RepeatModeBox.SelectedIndex != RepeatMode.Time || (RepeatMode)RepeatModeBox.SelectedIndex != RepeatMode.DayOfWeek;
                DateGrid.Visibility = ((RepeatMode)RepeatModeBox.SelectedIndex != RepeatMode.Time && !DayOfWeekGrid.IsEnabled) ? Visibility.Visible : Visibility.Hidden;
                if((RepeatMode)RepeatModeBox.SelectedIndex == RepeatMode.Time)
                {
                    var timeTransform = new TranslateTransform();

                    timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(TimeGrid.RenderTransform.Value.OffsetY, DateGrid.RenderTransform.Value.OffsetY, new Duration(TimeSpan.FromSeconds(.001))));
                    TimeGrid.RenderTransform = timeTransform;
                }
                if ((RepeatMode)RepeatModeBox.SelectedIndex != RepeatMode.Time && TimeGrid.RenderTransform.Value.OffsetY == DateGrid.RenderTransform.Value.OffsetY)
                {
                    var timeTransform = new TranslateTransform();

                    timeTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(TimeGrid.RenderTransform.Value.OffsetY, DateGrid.RenderTransform.Value.OffsetY+200, new Duration(TimeSpan.FromSeconds(.001))));
                    TimeGrid.RenderTransform = timeTransform;
                }
            }
        }

        private void HourOrMinuteBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (Convert.ToInt32(HourBox.Text) > 23) { e.Handled = true; HourBox.Text = "00"; HourBox.Focus(); HourBox.SelectAll(); }
                if (Convert.ToInt32(MinuteBox.Text) > 59) { e.Handled = true; MinuteBox.Text = "00"; MinuteBox.Focus(); MinuteBox.SelectAll(); }
            }
            catch { }

            if (!char.IsDigit(e.Text, e.Text.Length - 1) || e.Text.Length >= 2) e.Handled = true;
        }
    }
}
