/*
 * WineCooler.ino
 * Nick Stanley
 */

#include "application.h"

#define PIN_TMP36           1   //Analog Temperature Sensor TMP36
#define PIN_CHILL           2   //Relay for activating Peltier
#define PIN_FAN             5   //Relay for activating Fan (Peltier heatsink)
#define PIN_STATUS_CHILL    3   //LED status of Chiller
#define PIN_STATUS_SERVE    4   //LED status of ready-to-serve

/*
 * Lower and upper temperature thresholds in Centigrade
 * Per a lovely chart I found at
 * http://www.kj.com/blog/wine-101-what-temperature-should-my-wine-be
 * Index 0 = Storage
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
float WineTemp;     //Current temperature, as read by TMP36


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
    float volts = read * 3.3;
    volts /= 1024.0;
    float degreesC = (volts - 0.5) * 100;
    return degreesC;
}

/*
 *
 */
void ChillerOn()
{
    digitalWrite(PIN_STATUS_CHILL, HIGH);
}

/*
 *
 */
void ChillerOff()
{
    digitalWrite(PIN_STATUS_CHILL, LOW);
}

void setup()
{

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
