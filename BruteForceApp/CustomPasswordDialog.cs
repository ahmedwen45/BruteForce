using System.Windows;
using System.Windows.Controls;

namespace BruteForceApp
{
    /// <summary>
    /// Simple dialog for the user to enter a custom password (max 6 chars).
    /// </summary>
    public class CustomPasswordDialog : Window
    {
        private TextBox _input;
        public string Password { get; private set; }

        public CustomPasswordDialog()
        {
            Title = "Enter Custom Password";
            Width = 360;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.DimGray;

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Enter password (max 6 chars, letters + digits):",
                Foreground = System.Windows.Media.Brushes.White,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 8)
            });

            _input = new TextBox
            {
                MaxLength = 6,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 16,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 16)
            };
            panel.Children.Add(_input);

            var btnOk = new Button
            {
                Content = "OK",
                Width = 80,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(8, 4, 8, 4)
            };
            btnOk.Click += (s, e) =>
            {
                if (_input.Text.Length < 1)
                {
                    MessageBox.Show("Password cannot be empty.");
                    return;
                }
                Password = _input.Text;
                DialogResult = true;
            };
            panel.Children.Add(btnOk);

            Content = panel;
        }
    }
}
