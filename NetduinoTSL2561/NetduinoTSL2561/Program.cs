using System.Threading;
using Microsoft.SPOT;

using ToolBoxes;

namespace TestNetduinoTSL2561
{
    public class Program
    { // http://webge.github.io/TSL2561/
        public static void Main()
        {   // Création d'un objet TSL2561 
            // avec l'adresse 0x29 et la fréquence de bus F = 100kHz
            TSL2561 I2CLightSensor = new TSL2561();
            I2CLightSensor.Init(TSL2561.Gain.x1, TSL2561.IntegrationTime._13MS);
            // Réglage des seuils
            I2CLightSensor.threshLowhigh = 0x20; I2CLightSensor.threshLowLow = 0x10;
            I2CLightSensor.threshhighhigh = 0x40; I2CLightSensor.threshhighLow = 0x20;

            while (true)
            { // Affichage du contenu des registres
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
                if (I2CLightSensor.calculateLux() != 0)
                {
                    Debug.Print("Luminosité: " + I2CLightSensor.calculateLux() + " lux");
                }
                else
                {
                    Debug.Print("Sensor overload");
                }
                Thread.Sleep(3000);
            }
        }
    }
}
