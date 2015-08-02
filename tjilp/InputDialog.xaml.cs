using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace tjilp
{
    /// <summary>
    ///     Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog()
        {
            this.InitializeComponent();
            this.Loaded += this.InputDialog_Loaded;
            this.InitialOpacity = this.Opacity;
        }

        double InitialOpacity { get; set; }
        bool filledIn { get; set; }
        string Output
        {
            get { return this.InputBox.Text; }
        }

        public static string Prompt()
        {
            var output = "";
            do
            {
                var prompt = new InputDialog();
                prompt.ShowDialog();
                output = (prompt.filledIn) ? prompt.Output : "";
            } while (String.IsNullOrWhiteSpace(output));
            return output;
        }

        void InputDialog_Loaded(object sender, RoutedEventArgs e) { this.InputBox.Focus(); }

        void OK_Click(object sender, RoutedEventArgs e)
        {
            this.filledIn = true;
            this.Close();
        }

        void Cancel_Click(object sender, RoutedEventArgs e) { Environment.Exit(0); }
        void Window_Closed(object sender, EventArgs e) { if (!this.filledIn) Environment.Exit(0); }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var da = new DoubleAnimation();
                da.From = this.Opacity;
                da.To = 100;
                da.Duration = new Duration(TimeSpan.FromSeconds(100));
                this.BeginAnimation(OpacityProperty, da);
                this.DragMove();
                this.BeginAnimation(OpacityProperty, null);
            }
        }

        void InputBox_PreviewTextInput(object sender, TextCompositionEventArgs e) { if (this.InputBox.Text.Length >= 7 || !char.IsDigit(e.Text, e.Text.Length - 1)) e.Handled = true; }
    }
}