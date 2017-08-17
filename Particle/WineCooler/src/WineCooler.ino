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
int WineType = 0;   //Index for threshold temperatures, per chart above
double WineTemp;    //Current temperature, as read by TMP36

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
 */
int SetWineType(String type)
{
  WineType = type.toInt();
  return 0;
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
}

void loop()
{
  if(WineType == 0)
  {
      digitalWrite(PIN_STATUS_SERVE, HIGH);
  }
  else
  {
      digitalWrite(PIN_STATUS_SERVE, LOW);
  }
  WineTemp = ReadTempDegC(PIN_TMP36);
  if(WineTemp < LoThreshDegC[WineType])
  {
      ChillerOff();
  }
  if(WineTemp > HiThreshDegC[WineType])
  {
      ChillerOn();
  }
}
