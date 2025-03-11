using System.Windows;
using MahApps.Metro.Controls;

namespace FileDefender
{
    public partial class PasswordDialog : MetroWindow
    {
        public string Password { get; set; }

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}