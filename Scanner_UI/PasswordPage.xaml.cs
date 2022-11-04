using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PhistonUI;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanTest1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PasswordPage : Page
    {

        public PasswordPage()
        {
      
            this.InitializeComponent();
            Header.Text = Globals.passwordTitle;

        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            ////Debug.Print("Navigating to User Page\n");
            //Frame.Navigate(typeof(UserPage));
        }

        private void AddChar(string button_val)
        {
            if (PasswordCode.Text.Length < 6)
            {
                PasswordCode.Text += button_val;
            }
            UserMsg.Text = "";
        }

        private void RemoveChar()
        {
           if (PasswordCode.Text.Length > 0)
            {
                PasswordCode.Text = PasswordCode.Text.Substring(0, PasswordCode.Text.Length - 1);
            }
            UserMsg.Text = "";
        }


        private void B0_Click(object sender, RoutedEventArgs e)
        {
            AddChar("0");
        }
        private void B1_Click(object sender, RoutedEventArgs e)
        {
            AddChar("1");
        }
        private void B2_Click(object sender, RoutedEventArgs e)
        {
            AddChar("2");
        }
        private void B3_Click(object sender, RoutedEventArgs e)
        {
            AddChar("3");
        }
        private void B4_Click(object sender, RoutedEventArgs e)
        {
            AddChar("4");
        }
        private void B5_Click(object sender, RoutedEventArgs e)
        {
            AddChar("5");
        }
        private void B6_Click(object sender, RoutedEventArgs e)
        {
            AddChar("6");
        }
        private void B7_Click(object sender, RoutedEventArgs e)
        {
            AddChar("7");
        }
        private void B8_Click(object sender, RoutedEventArgs e)
        {
            AddChar("8");
        }
        private void B9_Click(object sender, RoutedEventArgs e)
        {
            AddChar("9");
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            RemoveChar();
        }
        private void Return_Click(object sender, RoutedEventArgs e)
        {

            Frame.Navigate(typeof(UserPage));


        }

        private void Enter_Click(object sender, RoutedEventArgs e)
        {
            if ((PasswordCode.Text.Length == 6))
            {
                if(PasswordCode.Text == Globals.passwordValue)
                {
                    switch (Globals.pageType)
                    {
                        case Globals.PAGE_TYPES.FACTORY_PAGE:
                            Globals.remote_refresh_request = true;
                            Frame.Navigate(typeof(FactoryConfigurePage));
                            break;
                        case Globals.PAGE_TYPES.SUPERVISOR_PAGE:
                            Globals.remote_refresh_request = true;
                            Frame.Navigate(typeof(ConfigureMachinePage));
                            break;
                        default:
                            Globals.remote_refresh_request = true;
                            Frame.Navigate(typeof(UserPage));
                            break;
                    }
                }
                else
                {
                    UserMsg.Text = "Password does not match.";
                    PasswordCode.Text = "";
                }
            }
            else
            {
                UserMsg.Text = "Password must be 6 digits.";
            }
        }
    }
}
