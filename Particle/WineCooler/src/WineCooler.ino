/*
 * WineCooler.ino
 * Nick Stanley
 */

#include "application.h"

//Pin definitions
#define PIN_TMP36           A0   //Analog Temperature Sensor TMP36
#define PIN_CHILL           D3   //Relay for activating Peltier
#define PIN_FAN             D4   //Relay for activating Fan (Peltier heatsink)
#define PIN_STATUS_CHILL    D0   //LED status of Chiller
#define PIN_STATUS_SERVE    D1   //LED status of ready-to-serve

//Constants
#define FAN_TIMEOUT   120000  //Keep fan running 2 extra minutes
#define SLEEP_TIME    300000  //Check in every 5 minutes

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
//                     0  1   2   3   4   5   6
int LoThreshDegC[] = {12, 7,  7,  9, 13, 15, 16};
int HiThreshDegC[] = {13, 8, 10, 11, 16, 16, 18};
int PrepTimeMins[] = {0, 30, 30, 30, 30, 30, 30};
int WineType = 1;   //Index for threshold temperatures, per chart above (1-5)
int WineMode = 0;   //Index for running type - Serve (0) or WineType
double WineTemp;    //Current temperature, as read by TMP36

/*
 *
 */
int currentTimeMinutes;
int serveTimeMinutes;
int serveDurationMinutes;

/*
 * FanOff()
 * Turns the fan relay off shortly after Peltier is deactivated
 */
void FanOff()
{
  digitalWrite(PIN_FAN, LOW);
}
Timer t_Fan(FAN_TIMEOUT, FanOff);

/*
 * ReadTempDegC()
 * Reads the given Pin and returns the temperature
 * in Centigrade
 * modeled from Adafruit's TMP36 tutorial
 * https://learn.adafruit.com/tmp36-temperature-sensor
 */
int ReadTempDegC(int pin)
{
  int read = analogRead(pin);
  double volts = read * 3.3;
  volts /= 1024.0;
  double degreesC = (volts - 0.5) * 100;
  return degreesC;
}

/*
 *
 */
void ChillerOn()
{
  //Peltier On
  digitalWrite(PIN_CHILL, HIGH);
  //Heatsink fan on
  digitalWrite(PIN_FAN, HIGH);
  t_Fan.stop();
  //Status light on
  digitalWrite(PIN_STATUS_CHILL, HIGH);
}

/*
 *
 */
void ChillerOff()
{
  //Peltier Off
  digitalWrite(PIN_CHILL, LOW);
  //Status light off
  digitalWrite(PIN_STATUS_CHILL, LOW);
  //Heatsink Fan off....later
  t_Fan.start();
}

/*
 * SetWineType()
 * Function cloud to change wine type for temperature management
 * Value contrained from 1 to 5
 */
int SetWineType(String type)
{
  WineType = constrain(type.toInt(), 1, 5);
  return 0;
}

/*
 * SetWineServeTime()
 * Sets the desired time to enjoy the wine
 * (serve temperature != store temperature)
 * Includes duration of serving window
 * ex. 1050_120 indicates a time of 17:30 for 2 hours
 * 17*60 + 30 = 1050, 2*60 = 120
 */
int SetWineServeTime(String time_duration)
{
  int delim = time_duration.indexOf('_');
  serveTimeMinutes = time_duration.substring(0,delim).toInt();
  serveDurationMinutes = time_duration.substring(delim+1).toInt();
  if(serveTimeMinutes != 0 && serveDurationMinutes !=0)
    return 0;
  else
    return -1;
}

void setup()
{
  //Pin modes
  pinMode(PIN_CHILL, OUTPUT);
  pinMode(PIN_FAN, OUTPUT);
  pinMode(PIN_STATUS_SERVE, OUTPUT);
  pinMode(PIN_STATUS_CHILL, OUTPUT);
  //Cloud functions
  Particle.variable("WineTemp", WineTemp);
  Particle.variable("WineType", WineType);
  Particle.function("SetWineType", SetWineType);
  Particle.function("SetWineTime", SetWineServeTime);
}

void loop()
{
  //Get the time and temperature
  currentTimeMinutes = Time.hour() * 60 + Time.minute();
  WineTemp = ReadTempDegC(PIN_TMP36);

  //If it's preperation time, swap to desired temperature range
  if( WineMode == 0 && ((currentTimeMinutes - PrepTimeMins[WineType]) >= serveTimeMinutes) && (currentTimeMinutes < (serveTimeMinutes + serveDurationMinutes)))
  {
    WineMode = WineType;
  }
  //If we've passed serving time, set back to storage temperature range
  if( WineMode != 0 && (currentTimeMinutes >= (serveTimeMinutes + serveDurationMinutes)))
  {
    WineMode = 0;
  }

  //If we're in serving temperature, indicate LED
  if((WineMode != 0) && (WineTemp >= LoThreshDegC[WineMode]) && (WineTemp <= HiThreshDegC[WineMode]))
  {
      digitalWrite(PIN_STATUS_SERVE, HIGH);
  }
  else
  {
      digitalWrite(PIN_STATUS_SERVE, LOW);
  }

  //Turn off chiller once we've gone under low threshold
  //It's too cold!
  if(WineTemp < LoThreshDegC[WineType])
  {
      ChillerOff();
  }
  //Turn on chiller once we've gone above high threshold
  //It's too hot!
  if(WineTemp > HiThreshDegC[WineType])
  {
      ChillerOn();
  }

  //Check in later
  System.sleep(SLEEP_TIME);
}
