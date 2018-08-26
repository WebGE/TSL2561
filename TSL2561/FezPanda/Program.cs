#define tft
using System.Threading;
using Microsoft.SPOT;

using Microtoolskit.Hardware.Sensors;

#if tft
using Microtoolskit.Hardware.Displays.TFTColor;
using GHI.Pins;
#endif

namespace FezPanda
{
    public class Program
    {
        // http://webge.github.io/TSL2561/
        public static void Main()
        {   // Creating TSL2561 object
            // with address 0x29 and bus frequency F = 100kHz
            TSL2561 I2CLightSensor = new TSL2561();
            I2CLightSensor.Init(TSL2561.Gain.x1, TSL2561.IntegrationTime._13MS);
            // Threshold adjustment
            I2CLightSensor.threshLowhigh = 0x20; I2CLightSensor.threshLowLow = 0x10;
            I2CLightSensor.threshhighhigh = 0x40; I2CLightSensor.threshhighLow = 0x20;
#if tft
            // Creating ST7735 object
            ST7735 Display = new ST7735(FEZPandaIII.Gpio.D8, FEZPandaIII.Gpio.D10, FEZPandaIII.SpiBus.Spi1);
            Display.DrawLargeText(40, 10, "TSL2561", Color.Yellow);
#endif
            while (true)
            {
#if tft
                Display.DrawText(10, 40, "Registres:", Color.Magenta);
                Display.DrawText(10, 50, "------------------- ", Color.Magenta);
                Display.DrawText(10, 60, "00h Control     : " + I2CLightSensor.control, Color.White);
                Display.DrawText(10, 70, "01h Timing      : " + I2CLightSensor.timing, Color.White);
                Display.DrawText(10, 80, "06h Interrupt   : " + I2CLightSensor.interrupt, Color.White);
                Display.DrawText(10, 90, "0Ah Part/Rev Id : " + I2CLightSensor.id, Color.White);
                Display.DrawText(10, 100, "------------------- ", Color.Magenta);
                Display.DrawText(10, 110, "Luminosite = " + I2CLightSensor.calculateLux() + " lux" , Color.White);
#else
                // Contents of registers
                Debug.Print("Lecture des registres");
                Debug.Print("---------------------");
                Debug.Print("00h Control              : " + I2CLightSensor.control);
                Debug.Print("01h Timing               : " + I2CLightSensor.timing);
                Debug.Print("02h ThreshLowLow         : " + I2CLightSensor.threshLowLow);
                Debug.Print("03h ThreshLowhigh        : " + I2CLightSensor.threshLowhigh);
                Debug.Print("04h ThreshhighLow        : " + I2CLightSensor.threshhighLow);
                Debug.Print("05h Threshhighhigh       : " + I2CLightSensor.threshhighhigh);
                Debug.Print("06h Interrupt            : " + I2CLightSensor.interrupt);
                Debug.Print("0Ah Part number / Rev Id : " + I2CLightSensor.id);
                Debug.Print("--------------------------");
                Debug.Print("Valeurs des canaux 0 et 1");
                Debug.Print("--------------------------");
                Debug.Print("Canal 0: " + I2CLightSensor.channel0);
                Debug.Print("Canal 1: " + I2CLightSensor.channel1);
#endif
                if (I2CLightSensor.calculateLux() != 0)
                {
                    Debug.Print("Luminosité: " + I2CLightSensor.calculateLux() + " lux");
                }
                else
                {
                    Debug.Print("Sensor overload");
                }
                Thread.Sleep(500);
            }
        }
    }
}
