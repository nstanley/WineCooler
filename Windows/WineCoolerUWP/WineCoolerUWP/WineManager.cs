using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Particle.SDK;
using Windows.Security.Credentials;
using Windows.System.Threading;
using Windows.UI.Core;
using Particle.SDK.Models;

namespace WineCoolerUWP
{
    public class WineManager
    {
        //Private properties
        private ParticleDevice pd_WineController;
        private string resourceName = "WineManager";
        private Exception uh_oh;
        private CoreDispatcher UIDispatcher;

        //Public properties
        public bool loginSuccess { get; private set; }
        public bool isReady { get; private set; }
        public string loginUser { get; private set; }
        public string loginPass { get; private set; }
        public double temperature { get; private set; }
        public int serveMinutes;
        public int serveDuration;
        public int wineType;
        public int systemMode;
        public int clockMinutes;
        public List<ParticleDevice> listParticleDevices { get; private set; }
        public string message { get; private set; }
    /*
     * ENUM
     */
    public String[] listWineTypes = new String[] {  "Storage",
                                                        "Champagne, Sparkling Wine",
                                                        "Pinot Gris, Riesling, Sauvignon Blanc",
                                                        "Chardonnay, Viognier, White Bordeaux",
                                                        "Pinot Noir",
                                                        "Cabernet Franc",
                                                        "Syrah, Zinfandel, Merlot, Malbec, Cabernet Sauvignon" };
        public int[] listServeDurations = new int[] { 1, 2, 3, 4 };
        public String[] listModes = new String[] { "Schedule", "Store", "Serve", "Off" };

        //Events
        public event Action<double> WineTempEvent;
        public event Action<int> ClockEvent;
        public event Action<bool> LoginChangeEvent;
        public event Action<List<ParticleDevice>> AvailableDevicesEvent;
        public event Action<Exception> ExceptionEvent;
        public event Action<bool> WineServeEvent;
        public event Action<int> SystemModeEvent;
        public event Action<string> DeviceChosenEvent;
        public event Action<string> MessageEvent;

        //Constructor
        public WineManager(CoreDispatcher dis)
        {
            isReady = false;
            //message = "Welcome";
            //MessageEvent(message);
            UIDispatcher = dis;
            //Login if possible (saved credentials
            var loginCreds = GetCredentialFromLocker();
            if (loginCreds != null)
            {
                loginCreds.RetrievePassword();
                Login(loginCreds.UserName, loginCreds.Password);
                loginUser = loginCreds.UserName;
                loginPass = loginCreds.Password;
            }
            else
            {
                //message = "Please log in";
                //MessageEvent(message);
            }
        }

        /// <summary>
        /// Assigns a ParticleDevice to our manager
        /// 
        /// </summary>
        /// <param name="pd"></param>
        private async Task SetController(ParticleDevice pd)
        {
            pd_WineController = pd;

            MessageEvent("Updating parameters...");
            //Get variables from the cloud
            try
            {
                //message = "Updating settings from cloud...";
                //MessageEvent(message);
                ParticleVariableResponse varResponse = await this.pd_WineController.GetVariableAsync("WineTemp");
                temperature = (double)varResponse.Result;

                varResponse = await this.pd_WineController.GetVariableAsync("WineType");
                wineType = (int)varResponse.Result;

                varResponse = await this.pd_WineController.GetVariableAsync("SysMode");
                systemMode = (int)varResponse.Result;

                varResponse = await this.pd_WineController.GetVariableAsync("ClockMins");
                clockMinutes = (int)varResponse.Result;

                varResponse = await this.pd_WineController.GetVariableAsync("ServeMins");
                serveMinutes = (int)varResponse.Result;

                varResponse = await this.pd_WineController.GetVariableAsync("ServeDur");
                serveDuration = (int)varResponse.Result;
            }
            catch
            {

            }


            //Listen to Particle events to notify user when wine is servalble
            Task<Guid> serveEventID = this.pd_WineController.SubscribeToDeviceEventsWithPrefixAsync(ServeHandler, "ServeReady");

            //Check in on the temperature periodically
            TimeSpan period = TimeSpan.FromSeconds(60);
            ThreadPoolTimer PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer( async (source) =>
            {
                ParticleVariableResponse varResponse = await this.pd_WineController.GetVariableAsync("WineTemp");
                temperature = (double)varResponse.Result;

                varResponse = await this.pd_WineController.GetVariableAsync("ClockMins");
                clockMinutes = (int)varResponse.Result;

                await UIDispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        //
                        // UI components can be accessed within this scope.
                        //
                        WineTempEvent(temperature);
                        ClockEvent(clockMinutes);
                    });
            }
            , period);

            //Notify UI
            DeviceChosenEvent(pd.Name);
            WineTempEvent(temperature);
            MessageEvent("Welcome");
            isReady = true;
        }
        
        /// <summary>
        /// Lets the UI know that we saw the "ServeReady" event come through the Particle cloud
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="particeEvent"></param>
        private void ServeHandler(object sender, ParticleEventResponse particeEvent)
        {
            WineServeEvent(true);
        }

        /// <summary>
        /// Attempts login to Particle Cloud. If successful, it (1) stores credentials, (2) gets device list and  lets UI know,
        /// and (3) tries to assign a device if it includes "wine" in the name
        /// </summary>
        /// <param name="user">Email address of the user</param>
        /// <param name="pass">Password of the user</param>
        public async void Login(string user, string pass)
        {
            //message = "Logging in...";
            //MessageEvent("Logging in...");
            try
            {
                loginSuccess = await ParticleCloud.SharedCloud.LoginAsync(user, pass);
            }
            catch (Exception ex)
            {
                uh_oh = ex;
                loginSuccess = false;
                LoginChangeEvent(loginSuccess);
                ExceptionEvent(uh_oh);
            }
            if (loginSuccess)
            {
                //Save login
                PasswordVault passVault= new PasswordVault();
                passVault.Add(new PasswordCredential(resourceName, user, pass));

                //Get device list from particle cloud
                //message = "Getting devices...";
                //MessageEvent(message);

                MessageEvent("Getting devices...");
                listParticleDevices = await ParticleCloud.SharedCloud.GetDevicesAsync();

                //Tell the UI
                AvailableDevicesEvent(listParticleDevices);

                //See if we can find a "wine" controller
                //Sets the controlled device to first found with "wine" in the name
                bool foundController = false;
                foreach (ParticleDevice device in listParticleDevices)
                {
                    if (device.Name.IndexOf("wine", StringComparison.OrdinalIgnoreCase) >= 0 && !foundController)
                    {
                        foundController = true;
                        //message = "Found " + device.Name;
                        //MessageEvent(message);
                        await this.SetController(device);
                    }
                }
            }
            else
            {
            }

            //Let UI know to enable/disable functionality
            LoginChangeEvent(loginSuccess);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="time"></param>
        public async Task WineSetup(string type, string time)
        {
            try
            {
                
                ParticleFunctionResponse functionResponseType = await pd_WineController.RunFunctionAsync("SetWineType", type);
                ParticleFunctionResponse functionResponseTime = await pd_WineController.RunFunctionAsync("SetWineTime", time);
                int resultType = functionResponseType.ReturnValue;
                int resultTime = functionResponseType.ReturnValue;
                if (resultTime < 0 || resultType < 0)
                {
                    //error from device
                }
                else
                {
                    //success
                }
            }
            catch (Exception ex)
            {
                uh_oh = ex;
                ExceptionEvent(uh_oh);
            }
        }

        public async Task SetWineType(string type)
        {
            try
            {
                ParticleFunctionResponse functionResponseType = await pd_WineController.RunFunctionAsync("SetWineType", type);
            }
            catch
            {

            }
        }

        public async Task SetWineTime(string time)
        {
            try
            {
                ParticleFunctionResponse functionResponseType = await pd_WineController.RunFunctionAsync("SetWineTime", time);
            }
            catch
            {

            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        public async Task SetSystemMode(string mode)
        {
            switch(mode)
            {
                case "Schedule":
                    systemMode = 0;
                    break;
                case "Store":
                    systemMode = 1;
                    break;
                case "Serve":
                    systemMode = 2;
                    break;
                case "Off":
                    systemMode = 3;
                    break;
                default:
                    systemMode = 1;
                    break;
            }
            try
            {
                ParticleFunctionResponse functionResponseTime = await pd_WineController.RunFunctionAsync("SetSysMode", systemMode.ToString());
            }
            catch(Exception ex)
            {
                uh_oh = ex;
                ExceptionEvent(uh_oh);
            }
            SystemModeEvent(systemMode);
        }

        public async Task SetSystemTimeZone(int zone)
        {
            try
            {
                ParticleFunctionResponse functionResponse = await pd_WineController.RunFunctionAsync("SetTimeZone", zone.ToString());
                clockMinutes = (int)functionResponse.ReturnValue;
            }
            catch(Exception ex)
            {
                uh_oh = ex;
                ExceptionEvent(uh_oh);
            }
        }

        /// <summary>
        /// Microsoft's example for getting stored credentials from a password vault
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                uh_oh = ex;
                ExceptionEvent(uh_oh);
                //probably didn't have a stored credential
            }
            return credential;
        }


    }//end class
}//end namesapce
