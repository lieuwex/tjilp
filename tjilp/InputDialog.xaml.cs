using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace tjilp
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        private double InitialOpacity { get; set; }
        private bool filledIn { get; set; }

        public InputDialog()
        {
            InitializeComponent();
            this.Loaded += InputDialog_Loaded;
            this.InitialOpacity = this.Opacity;
        }

        public static string Prompt()
        {
            string output = "";

            do
            {
                var prompt = new InputDialog();
                prompt.ShowDialog();
                output = (prompt.filledIn) ? prompt.Output : "";
            } while (String.IsNullOrWhiteSpace(output));

            return output;
        }

        string Output
        {
            get { return InputBox.Text; }
        }

        void InputDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InputBox.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.filledIn = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!this.filledIn) Environment.Exit(0);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
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

        private void InputBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (InputBox.Text.Length >= 7 || !char.IsDigit(e.Text, e.Text.Length - 1)) e.Handled = true;
        }
    }
}