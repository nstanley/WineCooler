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
using Windows.System.Threading;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WineCoolerUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        WineManager wineFridge;
        
        public MainPage()
        {

            //Setup our manager and watch for events
            wineFridge = new WineManager(Dispatcher);
            wineFridge.LoginChangeEvent += new Action<bool>(UpdateLogin);
            wineFridge.WineTempEvent += new Action<double>(UpdateTemperature);
            wineFridge.AvailableDevicesEvent += new Action<List<ParticleDevice>>(UpdateDeviceList);
            wineFridge.ExceptionEvent += new Action<Exception>(ShowException);
            wineFridge.SystemModeEvent += new Action<int>(UpdateSystemMode);
            wineFridge.DeviceChosenEvent += new Action<string>(UpdateDevice);
            wineFridge.ClockEvent += new Action<int>(UpdateClock);
            wineFridge.MessageEvent += new Action<string>(UpdateMessage);

            this.InitializeComponent();
            pvtFridge.IsEnabled = false;

            /*
            Binding messsageBind = new Binding();
            messsageBind.Path = new PropertyPath("message");
            messsageBind.Mode = BindingMode.OneWay;
            messsageBind.Source = wineFridge;
            lblMessage.SetBinding(TextBlock.TextProperty, messsageBind);
            */

            //ParticleCloud.SharedCloud.SynchronizationContext = System.Threading.SynchronizationContext.Current;
            
            //Add wine types 1-6 (item 0 is Storage temp, not for serving)
            for (int i = 1; i < wineFridge.listWineTypes.Length; ++i)
            {
                cboWineType.Items.Add(wineFridge.listWineTypes[i]);
            }
            //cboWineType.SelectedItem = cboWineType.Items[3];

            //Add serving durations
            for(int i = 0; i < wineFridge.listServeDurations.Length; ++i)
            {
                cboDuration.Items.Add(wineFridge.listServeDurations[i]);
            }
            //cboDuration.SelectedItem = cboDuration.Items[4];

            //Add system modes
            for (int i = 0; i < wineFridge.listModes.Length; ++i)
            {
                cboMode.Items.Add(wineFridge.listModes[i]);
            }
            //cboMode.SelectedItem = cboMode.Items[0];

            for(int i = -12; i <= 14; ++i)
            {
                cboTime.Items.Add(i);
            }
        }

        #region WineManager Events
        private void UpdateLogin(bool success)
        {
            cboDevice.IsEnabled = success;

            txtUserName.Text = wineFridge.loginUser;
            txtPassword.Password = wineFridge.loginPass;
        }

        private void UpdateMessage(string s)
        {
            lblMessage.Text = s;
        }
        private void UpdateTemperature(double temp)
        {
            lblTemperature.Text = "Temperature: " + Math.Round(temp, 2).ToString() + "°C";
        }

        private void UpdateDeviceList(List<ParticleDevice> lpd)
        {
            foreach (ParticleDevice device in lpd)
            {
                cboDevice.Items.Add(device.Name);
            }
        }

        private async void ShowException(Exception ex)
        {
            var md = new MessageDialog("An issue has occurred!", "Uh oh...");
            await md.ShowAsync();
        }

        private void UpdateSystemMode(int mode)
        {
            cboMode.SelectedIndex = mode;
            if(mode == 0)
            {
                cboDuration.IsEnabled = true;
                timePicker.IsEnabled = true;
            }
            else
            {
                cboDuration.IsEnabled = false;
                timePicker.IsEnabled = false;
            }
        }

        
        private void UpdateDevice(string name)
        {
            pvtFridge.IsEnabled = true;
            cboDevice.SelectedIndex = cboDevice.Items.IndexOf(name);
            cboWineType.SelectedIndex = wineFridge.wineType - 1;
            cboMode.SelectedIndex = wineFridge.systemMode;
            cboDuration.SelectedIndex = (wineFridge.serveDuration / 60) - 1;
            timePicker.Time = new TimeSpan(0, wineFridge.serveMinutes,0);
            txtTime.Text = new TimeSpan(0, wineFridge.clockMinutes, 0).ToString(@"hh\:mm");
            if (wineFridge.systemMode == 0)
            {
                cboDuration.IsEnabled = true;
                timePicker.IsEnabled = true;
            }
            else
            {
                cboDuration.IsEnabled = false;
                timePicker.IsEnabled = false;
            }
        }

        private void UpdateClock(int totalMinutes)
        {
            txtTime.Text = new TimeSpan(0, totalMinutes, 0).ToString(@"hh\:mm");
        }

        #endregion

        #region Button Events
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            wineFridge.Login(txtUserName.Text, txtPassword.Password);
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            //pd_WineDevice = await ParticleCloud.SharedCloud.GetDeviceAsync(cboDevice.SelectedItem.ToString());
        }

        private void btnWineSetup_Click(object sender, RoutedEventArgs e)
        {
            //String TypeFormat = (cboWineType.SelectedIndex + 1).ToString();
            //String TimeFormat = timePicker.Time.TotalMinutes.ToString() + "_" + (((int)cboDuration.SelectedIndex + 1)*60).ToString();
            //wineFridge.WineSetup(TypeFormat, TimeFormat);
        }

        private async void cboMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (wineFridge.isReady)
            {
                await wineFridge.SetSystemMode(cboMode.SelectedItem.ToString());
            }
        }

        private async void btnTime_Click(object sender, RoutedEventArgs e)
        {
            if (wineFridge.isReady)
            {
                await wineFridge.SetSystemTimeZone(cboTime.SelectedIndex - 12);
                txtTime.Text = new TimeSpan(0, wineFridge.clockMinutes, 0).ToString(@"hh\:mm");
            }
        }

        private async void cboDuration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (wineFridge.isReady)
            {
                String TimeFormat = timePicker.Time.TotalMinutes.ToString() + "_" + (((int)cboDuration.SelectedIndex + 1)*60).ToString();
                await wineFridge.SetWineTime(TimeFormat);
            }
        }

        private async void timePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (wineFridge.isReady)
            {
                String TimeFormat = timePicker.Time.TotalMinutes.ToString() + "_" + (((int)cboDuration.SelectedIndex + 1)*60).ToString();
                await wineFridge.SetWineTime(TimeFormat);
            }
        }

        private async void cboWineType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (wineFridge.isReady)
            {
                String TypeFormat = (cboWineType.SelectedIndex + 1).ToString();
                await wineFridge.SetWineType(TypeFormat);
            }
        }

        #endregion


    }
}
