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
using System.Threading.Tasks;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScanTest1
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class UserIDPage : Page
    {



        public UserIDPage()
        {
            this.InitializeComponent();

            //Update the fields
            Globals.remote_refresh_request = true;

        }

               
        private void AddChar(string button_val)
        {
            if(UserID.FocusState != FocusState.Unfocused)
            {
                if(UserID.Text.Length < 4)
                    UserID.Text += button_val;
            }
            else if (DateCode.FocusState != FocusState.Unfocused)
            {
                if(DateCode.Text.Length <6)
                {
                    DateCode.Text += button_val;
                }
            }
            UserMsg.Text = "";
        }

        private void RemoveChar()
        {
            if (UserID.FocusState != FocusState.Unfocused)
            {
                if (UserID.Text.Length > 0)
                {
                    UserID.Text = UserID.Text.Substring(0, (UserID.Text.Length - 1));
                }
            }
            else if (DateCode.FocusState != FocusState.Unfocused)
            {
                if (DateCode.Text.Length > 0)
                {
                    DateCode.Text = DateCode.Text.Substring(0, (DateCode.Text.Length - 1));
                }
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
            if (!((DateCode.Text.Length == 6) && (UserID.Text.Length == 4)))
            {
                // Not properly formated
                Globals.date_code = "";
                Globals.user_id = "";
            }

            Frame.Navigate(typeof(UserPage));
        }
        private void Set_Click(object sender, RoutedEventArgs e)
        {
            if((DateCode.Text.Length == 6) && (UserID.Text.Length == 4))
            {
                Globals.date_code = DateCode.Text;
                Globals.user_id = UserID.Text;
                UserMsg.Text = "Values have been set.";

                //User is now logged in
            }
            else
            {
                UserMsg.Text = "User ID must be 4 digits, Date Code must be 6 digits.";
            }
        }


    }
}
