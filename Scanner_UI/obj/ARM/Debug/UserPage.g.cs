#pragma checksum "D:\OneDrive - Proton Design Inc\Documents\GitHub\Phiston\Scanner UI\UserPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "C1EBEC92083D6782582AE842962B4EE9"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ScanTest1
{
    partial class UserPage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.18362.1")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1: // UserPage.xaml line 1
                {
                    global::Windows.UI.Xaml.Controls.Page element1 = (global::Windows.UI.Xaml.Controls.Page)(target);
                    ((global::Windows.UI.Xaml.Controls.Page)element1).Loaded += this.OnLoad;
                }
                break;
            case 2: // UserPage.xaml line 12
                {
                    this.UserLayout = (global::Windows.UI.Xaml.Controls.Grid)(target);
                }
                break;
            case 3: // UserPage.xaml line 55
                {
                    this.ResultsBorder = (global::Windows.UI.Xaml.Controls.Border)(target);
                }
                break;
            case 4: // UserPage.xaml line 68
                {
                    this.Results = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 5: // UserPage.xaml line 71
                {
                    this.ConfirmPopup = (global::Windows.UI.Xaml.Controls.Primitives.Popup)(target);
                }
                break;
            case 6: // UserPage.xaml line 120
                {
                    this.UserIDBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 7: // UserPage.xaml line 121
                {
                    this.DateCodeBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 8: // UserPage.xaml line 125
                {
                    this.StorageDriveBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 9: // UserPage.xaml line 126
                {
                    this.LinkStatusBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 10: // UserPage.xaml line 127
                {
                    this.RXDeviceStatus = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 11: // UserPage.xaml line 132
                {
                    this.image = (global::Windows.UI.Xaml.Controls.Image)(target);
                }
                break;
            case 12: // UserPage.xaml line 139
                {
                    this.ButtonPanel = (global::Windows.UI.Xaml.Controls.StackPanel)(target);
                }
                break;
            case 13: // UserPage.xaml line 162
                {
                    this.CycleCountBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 14: // UserPage.xaml line 165
                {
                    this.ZeroButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ZeroButton).Click += this.ZeroButton_Click;
                }
                break;
            case 15: // UserPage.xaml line 179
                {
                    this.UserMsg = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 16: // UserPage.xaml line 142
                {
                    this.ResetButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ResetButton).Click += this.ResetButton_Click;
                }
                break;
            case 17: // UserPage.xaml line 145
                {
                    this.ScanButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ScanButton).Click += this.ScanButton_Click;
                }
                break;
            case 18: // UserPage.xaml line 149
                {
                    this.ManScanButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ManScanButton).Click += this.ManScanButton_Click;
                }
                break;
            case 19: // UserPage.xaml line 153
                {
                    this.LogInButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.LogInButton).Click += this.LogInButton_Click;
                }
                break;
            case 20: // UserPage.xaml line 155
                {
                    this.ConfigureButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ConfigureButton).Click += this.ConfigureButton_Click;
                }
                break;
            case 21: // UserPage.xaml line 146
                {
                    this.ScanButtonText = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 22: // UserPage.xaml line 78
                {
                    global::Windows.UI.Xaml.Controls.Button element22 = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)element22).Click += this.PopContinueButton_Click;
                }
                break;
            case 23: // UserPage.xaml line 79
                {
                    global::Windows.UI.Xaml.Controls.Button element23 = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)element23).Click += this.PopEjectButton_Click;
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.18362.1")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

