#include "chiller.h"

WineChiller::WineChiller(int pelt, int circ, int temp, int sph, int spl)
{
    pin_peltier = pelt;
    pin_fan_circulate = circ;
    pin_wine_temp = temp;
    sp_high = sph;
    sp_low = spl;
}

/*
 * ReadTempDegC()
 * Reads the given Pin and returns the temperature
 * in Centigrade
 * modeled from Adafruit's TMP36 tutorial
 * https://learn.adafruit.com/tmp36-temperature-sensor
 */
double WineChiller::ReadTempDegC()
{
  int read = analogRead(pin_wine_temp);
  double volts = read * 3.3 / 4096.0;    //3.3v ref voltage, 12bit resolution ADC
  double degreesC = (volts - 0.5) * 100.0;
  return degreesC;
}

double WineChiller::Update()
{

    //System off, turn off Peltier and air circulation
    digitalWrite(pin_fan_circulate, LOW);
    digitalWrite(pin_peltier, LOW);
    delay(50);
    wine_temp = ReadTempDegC();

    if(systemOn)
    {
        //Make sure Peltier is on
        //digitalWrite(pin_peltier, HIGH);
        //Turn off chiller once we've gone under low threshold
        //It's too cold!
        if(wine_temp < sp_low)
        {
            digitalWrite(pin_fan_circulate, LOW);
            digitalWrite(pin_peltier, LOW);
        }
        //Turn on chiller once we've gone above high threshold
        //It's too hot!
        if(wine_temp > sp_high)
        {
            digitalWrite(pin_fan_circulate, HIGH);
            digitalWrite(pin_peltier, HIGH);
        }
    }
    else
    {
        //System off, turn off Peltier and air circulation
        digitalWrite(pin_fan_circulate, LOW);
        digitalWrite(pin_peltier, LOW);
    }

    return wine_temp;
}

void WineChiller::UpdateSP(int sph, int spl)
{
    if(sp_high != sph && sp_low != spl)
    {
        sp_high = sph;
        sp_low = spl;
        this->Update();
    }
}

void WineChiller::UpdatePower(bool power)
{
    if(systemOn != power)
    {
        systemOn = power;
        this->Update();
    }
}
