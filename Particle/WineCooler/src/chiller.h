#include "application.h"

class WineChiller
{
    public:
        double wine_temp;
        WineChiller(int pelt, int circ, int temp, int sph, int spl);
        double Update();
        void UpdateSP(int a, int b);
        void UpdatePower(bool p);
    private:
        int pin_peltier;
        int pin_fan_circulate;
        int pin_wine_temp;
        int sp_high;
        int sp_low;
        bool systemOn;
        void SetPelteir(int s);
        void SetCirculate(int s);
        double ReadTempDegC();
};
