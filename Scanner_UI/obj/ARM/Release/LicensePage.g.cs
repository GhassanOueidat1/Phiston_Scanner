#pragma checksum "D:\OneDrive - Proton Design Inc\Documents\GitHub\Phiston\Scanner UI\LicensePage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "F0AF6CDB09CDF69DF9F9EBA3071B7B20"
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
    partial class LicensePage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.17.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 2: // LicensePage.xaml line 11
                {
                    this.LicenseLayout = (global::Windows.UI.Xaml.Controls.Grid)(target);
                }
                break;
            case 3: // LicensePage.xaml line 42
                {
                    this.TimeBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 4: // LicensePage.xaml line 49
                {
                    this.LicenseKey = (global::Windows.UI.Xaml.Controls.TextBox)(target);
                }
                break;
            case 5: // LicensePage.xaml line 51
                {
                    this.SaveLicenseButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.SaveLicenseButton).Click += this.SaveLicenseButton_Click;
                }
                break;
            case 6: // LicensePage.xaml line 52
                {
                    this.ReturnButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.ReturnButton).Click += this.ReturnButton_Click;
                }
                break;
            case 7: // LicensePage.xaml line 57
                {
                    this.StatusBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 8: // LicensePage.xaml line 61
                {
                    this.LicenseStatusBox = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 9: // LicensePage.xaml line 65
                {
                    this.AboutButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    ((global::Windows.UI.Xaml.Controls.Button)this.AboutButton).Click += this.AboutButton_Click;
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
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.17.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

