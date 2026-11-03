using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MottoLaundry.Views.LogIn
{
    /// <summary>
    /// Interaction logic for LogInView.xaml
    /// </summary>
    public partial class LogInView : UserControl
    {
        public LogInView()
        {
            InitializeComponent();
        }
        
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string usernameLogin = UsernameTextBox.Text;
            string passwordLogin = PasswordInput.Password;

            if (usernameLogin == "admin" && passwordLogin == "123")
            {
                MainWindow mainWindow = Window.GetWindow(this) as MainWindow;

                if (mainWindow != null)
                {
                    mainWindow.SwitcView(new Views.CustomerRecord.CustomerRecordView());
                }
            }
            else
            {
                MessageBox.Show("Invalid username or password. Please try again.");
            }

        }
    }
}
