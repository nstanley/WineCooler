/*
 * WineCooler.ino
 * Nick Stanley
 */

#include "application.h"
#include "chiller.h"

//Pin definitions
#define PIN_TMP36           A0   //Analog Temperature Sensor TMP36
#define PIN_CHILL           D3   //Relay for activating Peltier + heatsink fan
#define PIN_FAN             D4   //Relay for activating circulation fan inside fridge
#define PIN_STATUS_CHILL    D0   //LED status of Chiller
#define PIN_STATUS_SERVE    D1   //LED status of ready-to-serve

//Constants
#define TIME_CHECK_IN    30000  //Chiller cycle
#define SYSMODE_SCHEDULE     0  //Check time and prepare wine for serving temperature
#define SYSMODE_STORE        1  //Store wine at 55F
#define SYSMODE_SERVE        2  //Serve wine at specified temps per wine
#define SYSMODE_OFF          3  //Turn chiller off

/*
 * Lower and upper temperature thresholds in Centigrade
 * Per a lovely chart I found at
 * http://www.kj.com/blog/wine-101-what-temperature-should-my-wine-be
 * Index 0 = Storage (55F)
 * Index 1 = Champagne, Sparkling
 * Index 2 = Pinot Gris, Riesling, Sauv Blanc
 * Index 3 = Chardonnay, Viognier, White Bordeaux
 * Index 4 = Pinot Noir
 * Index 5 = Cab Franc
 * Index 6 = Syrah, Zinfandel, Merlot, Malbec, Cab Sauv
 */
//                                0    1    2   3     4     5     6
const double LoThreshDegC[] = {11.8, 6.2, 7.2,  9, 12.8, 14.6, 15.6};
const double HiThreshDegC[] = {13.8, 8.2,  10, 11, 15.6, 16.6, 18.3};
const int PrepTimeMins[]    = {   0,  30,  30, 30,   30,   30,   30};
int setpointIndex = 0;  //Index for running type - Serve (0) or WineType
int WineType = 4;       //Index for threshold temperatures, per chart above (1-6)
double WineTemp;        //Current temperature, as read by TMP36
int sysMode = 0;        //System mode - schedule, store, serve, off
bool serveBlink = false;

/*
 * Our wine cooler class to manage fans, setpoints, etc
 */
WineChiller wineFridge(PIN_CHILL, PIN_FAN, PIN_TMP36, HiThreshDegC[0], LoThreshDegC[0]);

/*
 * Timing variables
 */
int currentTimeMinutes;         //Current time of day, expresssed as total minutes
int serveTimeMinutes = 1020;    //Default time 5pm (17*60)
int serveDurationMinutes = 120; //Default serve for 2 hours
long lastCheckIn = 0;           //millis() counter for checking wine every interval

/*
 * SetWineType()
 * Cloud function to change wine type for temperature management
 * Value contrained from 1 to 6
 *
 * Returns an integer representing the wine type
 */
int SetWineType(String type)
{
  WineType = constrain(type.toInt(), 1, 6);
  return WineType;
}

/*
 * SetSysMode()
 * Cloud function to update the chiller's running mode
 * Value contrained from 0 to 3
 *
 * Returns integer for which zone
 */
int SetSysMode(String args)
{
  sysMode = constrain(args.toInt(), 0, 3);
  return sysMode;
}

/*
 * SetWineServeTime()
 * Sets the desired time to enjoy the wine
 * (serve temperature != store temperature)
 * Includes duration of serving window
 * ex. 1050_120 indicates a time of 17:30 for 2 hours
 * 17*60 + 30 = 1050, 2*60 = 120
 *
 * Returns the serve time expressed as total minutes
 */
int SetWineServeTime(String time_duration)
{
  int delim = time_duration.indexOf('_');
  serveTimeMinutes = time_duration.substring(0,delim).toInt();
  serveDurationMinutes = time_duration.substring(delim+1).toInt();
  if(serveTimeMinutes != 0 && serveDurationMinutes !=0)
    return serveTimeMinutes;
  else
    return -1;
}

/*
 * SetTimeZone()
 * Set the time zone for the controller for localization
 *
 * Returns the current time (adjusted by zone)
 */
int SetTimeZone(String zone)
{
    Time.zone(constrain(zone.toInt(), -12, 14));
    return ((Time.hour() * 60) + Time.minute());
}

/*
 * setup()
 * Set pin modes, expose functions and variable to cloud, init system
 */
void setup()
{
  //Pin modes
  pinMode(PIN_CHILL, OUTPUT);
  pinMode(PIN_FAN, OUTPUT);
  pinMode(PIN_STATUS_SERVE, OUTPUT);
  pinMode(PIN_STATUS_CHILL, OUTPUT);
  //Cloud variables
  Particle.variable("WineTemp", WineTemp);
  Particle.variable("WineType", WineType);
  Particle.variable("SysMode", sysMode);
  Particle.variable("ClockMins", currentTimeMinutes);
  Particle.variable("ServeMins", serveTimeMinutes);
  Particle.variable("ServeDur", serveDurationMinutes);
  //Cloud functions
  Particle.function("SetSysMode", SetSysMode);
  Particle.function("SetWineType", SetWineType);
  Particle.function("SetWineTime", SetWineServeTime);
  Particle.function("SetTimeZone", SetTimeZone);

  //I'm in Central DST at time of coding
  Time.zone(-5);

  //turn system on
  wineFridge.UpdatePower(true);
}

/*
 * loop()
 * Check in every cycle and process how to handle the wine based on scheduled mode
 *
 */
void loop()
{
  //Get the time
  currentTimeMinutes = (Time.hour() * 60) + Time.minute();
  switch (sysMode)
  {
      case SYSMODE_SCHEDULE:
        //If it's preperation time, swap to new desired temperature range
        if((currentTimeMinutes >= (serveTimeMinutes - PrepTimeMins[WineType])) && setpointIndex == 0)
        {
            setpointIndex = WineType;
        }
        //If we've passed serving time, set back to storage temperature range
        if((currentTimeMinutes >= (serveTimeMinutes + serveDurationMinutes)) && setpointIndex != 0)
        {
            setpointIndex = 0;
        }
        wineFridge.UpdateSP(HiThreshDegC[setpointIndex], LoThreshDegC[setpointIndex]);
        wineFridge.UpdatePower(true);
        break;

      case SYSMODE_STORE:
        setpointIndex = 0;
        wineFridge.UpdateSP(HiThreshDegC[setpointIndex], LoThreshDegC[setpointIndex]);
        wineFridge.UpdatePower(true);
        break;

      case SYSMODE_SERVE:
        setpointIndex = WineType;
        wineFridge.UpdateSP(HiThreshDegC[setpointIndex], LoThreshDegC[setpointIndex]);
        wineFridge.UpdatePower(true);
        break;

      case SYSMODE_OFF:
        wineFridge.UpdatePower(false);
        break;
  }

  //Evaluate chiller logic every couple of minutes
  if(millis() > lastCheckIn + TIME_CHECK_IN )
  {
      WineTemp = wineFridge.Update();
      lastCheckIn = millis();
  }

  //Take a break
  delay(200);

  /*/If we're in serving temperature, indicate LED
  if((setpointIndex != 0) && (WineTemp >= LoThreshDegC[setpointIndex]) && (WineTemp <= HiThreshDegC[setpointIndex]))
  {
      digitalWrite(PIN_STATUS_SERVE, HIGH);
  }
  else if(setpointIndex !=0) //&& temps outside range
  {
      digitalWrite(PIN_STATUS_SERVE, serveBlink);
      serveBlink = !serveBlink;
  }
  else
  {
      digitalWrite(PIN_STATUS_SERVE, LOW);
  }*/
}
