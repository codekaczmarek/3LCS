using System.Windows;
using System.Windows.Input;

namespace ThreeLCS.Views
{
    public partial class InputDialog : Window
    {
        public string? Result { get; private set; }

        public InputDialog(string prompt, string title, string currentValue)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputBox.Text = currentValue;
            Loaded += (_, _) => { InputBox.SelectAll(); InputBox.Focus(); };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Result = InputBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Result = InputBox.Text; DialogResult = true; }
        }
    }
}
