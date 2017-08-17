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
using Particle.SDK;
using Particle.SDK.Models;
using Particle.SDK.RestApi;
using Particle.SDK.Utils;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WineCoolerUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //The particle controller
        ParticleDevice pd_WineDevice;

        //List of wine types
        //Index matches arrays on Particle end
        String[] listWineTypes = new String[] {"Storage", "Champagne", "Riesling", "Chardonnay", "Pinot Noir", "Cabernet Franc", "Syrah" };

        //Options for serve duration
        int[] listServeDurations = new int[] { 1, 2, 3, 4 };

        public MainPage()
        {
            this.InitializeComponent();

            //ParticleCloud.SharedCloud.SynchronizationContext = System.Threading.SynchronizationContext.Current;

            //Add wine types 1-6 (item 0 is Storage temp, not for serving)
            for (int i = 1; i <= 6; ++i)
            {
                cboWineType.Items.Add(listWineTypes[i]);
            }

            //Add serving durations
            for(int i = 0; i < 4; ++i)
            {
                cboDuration.Items.Add(listServeDurations[i]);
            }
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var success = await ParticleCloud.SharedCloud.LoginAsync(txtUserName.Text, txtPassword.Password);
        }

        private async void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            pd_WineDevice = await ParticleCloud.SharedCloud.GetDeviceAsync(txtDevice.Text);
        }


    }
}
