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
using Windows.UI.Popups;
using Windows.Security.Credentials;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WineCoolerUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Available controllers
        List<ParticleDevice> listParticleDevices;

        //The particle controller
        ParticleDevice pd_WineDevice;

        bool loginSuccess = false;

        //List of wine types
        //Index matches arrays on Particle end
        String[] listWineTypes = new String[] {"Storage", "Champagne", "Riesling", "Chardonnay", "Pinot Noir", "Cabernet Franc", "Syrah" };

        //Options for serve duration
        int[] listServeDurations = new int[] { 1, 2, 3, 4 };

        String resourceName = "WineCoolerUWP";

        public MainPage()
        {
            this.InitializeComponent();

            //ParticleCloud.SharedCloud.SynchronizationContext = System.Threading.SynchronizationContext.Current;
            
            var loginCreds = GetCredentialFromLocker();
            if(loginCreds != null)
            {
                loginCreds.RetrievePassword();
                txtUserName.Text = loginCreds.UserName;
                txtPassword.Password = loginCreds.Password;
            }


            //Add wine types 1-6 (item 0 is Storage temp, not for serving)
            for (int i = 1; i <= 6; ++i)
            {
                cboWineType.Items.Add(listWineTypes[i]);
            }
            cboWineType.SelectedItem = cboWineType.Items[3];

            //Add serving durations
            for(int i = 0; i < 4; ++i)
            {
                cboDuration.Items.Add(listServeDurations[i]);
            }
            cboDuration.SelectedItem = cboDuration.Items[1];
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                loginSuccess = await ParticleCloud.SharedCloud.LoginAsync(txtUserName.Text, txtPassword.Password);
            }
            catch(Exception ex)
            {
                var md = new MessageDialog("Login failed!");                
                await md.ShowAsync();
            }
            if(loginSuccess)
            {
                //Save login
                var vault = new PasswordVault();
                vault.Add(new PasswordCredential(resourceName, txtUserName.Text, txtPassword.Password));

                //Get device list from particle cloud
                listParticleDevices = await ParticleCloud.SharedCloud.GetDevicesAsync();
                int i = 0;
                foreach (ParticleDevice device in listParticleDevices)
                {
                    cboDevice.Items.Add(device.Name);
                    if(device.Name == "WineCooler")
                    {
                        cboDevice.SelectedItem = cboDevice.Items[i];
                    }
                    ++i;
                }
            }
        }

        private PasswordCredential GetCredentialFromLocker()
        {
            PasswordCredential credential = null;

            var vault = new PasswordVault();
            try
            {
                var credentialList = vault.FindAllByResource(resourceName);
                if (credentialList.Count > 0)
                {
                    credential = credentialList[0];
                }
            }
            catch(Exception ex)
            {
                //probably didn't have a stored credential
            }
            return credential;
        }

        private async void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            pd_WineDevice = await ParticleCloud.SharedCloud.GetDeviceAsync(cboDevice.SelectedItem.ToString());
        }

        private async void btnWineSetup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String TypeFormat = (cboWineType.SelectedIndex + 1).ToString();
                String TimeFormat = timePicker.Time.TotalMinutes.ToString() + "_" + ((int)cboDuration.SelectedIndex + 1).ToString();
                ParticleFunctionResponse functionResponseType = await pd_WineDevice.RunFunctionAsync("SetWineType", TypeFormat);
                ParticleFunctionResponse functionResponseTime = await pd_WineDevice.RunFunctionAsync("SetWineTime", TimeFormat);
                int resultType = functionResponseType.ReturnValue;
                int resultTime = functionResponseType.ReturnValue;
                if (resultTime < 0 || resultType < 0)
                {
                    var md = new MessageDialog("There was an issue calling the functions!");
                    await md.ShowAsync();
                }
                else
                {
                    var md = new MessageDialog("Wine Cooler Updated!");
                    await md.ShowAsync();
                }
            }
            catch(Exception ex)
            {
                var md = new MessageDialog("There was an issue calling the functions!");
                await md.ShowAsync();
            }
        }

        private async void btnRefreshTemp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleVariableResponse varResponse = await pd_WineDevice.GetVariableAsync("WineTemp");
                double temp = (double)varResponse.Result;
                lblTemperature.Text = "Temperature: " + temp.ToString() + "°C";
            }
            catch(Exception ex)
            {
                var md = new MessageDialog("There was an issue getting the temperature!");
                await md.ShowAsync();
            }
        }
    }
}
